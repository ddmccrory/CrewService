using System.Text.Json;
using System.Text.Json.Nodes;
using CrewService.Domain.Models.Employees;
using CrewService.Domain.Modules.Authorization;
using CrewService.Domain.Modules.Employees;
using CrewService.Domain.Modules.Notifications;
using CrewService.Domain.Modules.Boards;
using CrewService.Domain.Modules.TenantConfig;
using CrewService.Domain.Modules.WorkManagement;
using CrewService.Domain.Modules.Workflows;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.Workflows;

public sealed class WorkflowTemplateManagementService(
    IWorkflowTemplateRepository workflowTemplateRepository,
    IWorkflowVersionRepository workflowVersionRepository,
    IWorkflowExecutionHistoryRepository workflowExecutionHistoryRepository,
    IWorkflowTriggerTypeRepository workflowTriggerTypeRepository,
    IWorkflowEffectTypeRepository workflowEffectTypeRepository,
    IWorkflowOperatorTypeRepository workflowOperatorTypeRepository,
    IWorkflowMetadataFieldTypeRepository workflowMetadataFieldTypeRepository,
    IRoleRepository roleRepository,
    IEmployeeRepository employeeRepository,
    IDynamicGroupRepository dynamicGroupRepository,
    IDepartmentRepository departmentRepository,
    ICraftRepository craftRepository,
    ISeniorityStateRepository seniorityStateRepository)
{
    public async Task<List<WorkflowTemplateSummary>> GetByRailroadAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        var templates = await workflowTemplateRepository.GetByRailroadAsync(railroadCtrlNbr, ct);
        var summaries = new List<WorkflowTemplateSummary>(templates.Count);

        foreach (var template in templates)
        {
            var versions = await workflowVersionRepository.GetByTemplateAsync(template.CtrlNbr, ct);
            var latestDraftVersion = versions
                .Where(v => string.Equals(v.Status, WorkflowVersionStatus.Draft, StringComparison.Ordinal))
                .OrderByDescending(v => v.SavedAtUtc)
                .ThenByDescending(v => v.VersionNumber)
                .FirstOrDefault();
            var latestPublishedVersion = versions
                .Where(v => string.Equals(v.Status, WorkflowVersionStatus.Published, StringComparison.Ordinal))
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();

            var latestVersion = latestDraftVersion ?? latestPublishedVersion;
            var latestStepCount = 0;
            if (latestVersion is not null)
            {
                var latestDefinition = DeserializeDefinition(latestVersion.DefinitionJson);
                latestStepCount = latestDefinition.Steps.Count;
            }

            summaries.Add(new WorkflowTemplateSummary(
                template.CtrlNbr,
                template.Name,
                template.TriggerTypeCtrlNbr is not null,
                template.TriggerTypeCtrlNbr,
                template.IsEnabled,
                latestDraftVersion is not null ? WorkflowVersionStatus.Draft : latestPublishedVersion?.Status ?? WorkflowVersionStatus.Draft,
                latestPublishedVersion?.VersionNumber ?? 0,
                latestStepCount));
        }

        return summaries;
    }

    public async Task<WorkflowStepFilterMetadataDto> GetStepFilterMetadataAsync(
        ControlNumber railroadCtrlNbr,
        ControlNumber triggerTypeCtrlNbr,
        List<WorkflowConditionValueDto> triggerConditions,
        CancellationToken ct = default)
    {
        var triggerType = await workflowTriggerTypeRepository.GetByCtrlNbrAsync(triggerTypeCtrlNbr, ct)
            ?? throw new InvalidOperationException($"Trigger type {triggerTypeCtrlNbr.Value} was not found.");

        var metadataFieldTypes = await workflowMetadataFieldTypeRepository.GetAllActiveAsync(ct);
        var metadataFieldTypeByCode = metadataFieldTypes.ToDictionary(field => field.Code, StringComparer.Ordinal);
        var metadataAllowedValuesByCode = await BuildMetadataAllowedValuesByCodeAsync(railroadCtrlNbr, ct);

        if (string.Equals(triggerType.Code, WorkflowTriggerTypeCodes.NotificationAccepted, StringComparison.Ordinal))
        {
            if (!metadataFieldTypeByCode.TryGetValue(WorkflowMetadataFieldTypeCodes.NotificationType, out var notificationTypeField))
                return new WorkflowStepFilterMetadataDto([]);

            var isBoardPlacementSelected = triggerConditions.Any(c =>
                c.FieldTypeCtrlNbr == notificationTypeField.CtrlNbr
                && string.Equals(c.Value, NotificationCategories.BoardPlacement, StringComparison.OrdinalIgnoreCase));

            if (!isBoardPlacementSelected)
                return new WorkflowStepFilterMetadataDto([]);

            return BuildStepFilterMetadata(
                [WorkflowMetadataFieldTypeCodes.BoardType],
                metadataFieldTypeByCode,
                metadataAllowedValuesByCode);
        }

        var mappedFieldCodes = GetDefaultMetadataFieldCodesForTrigger(triggerType.Code);
        if (mappedFieldCodes.Count == 0)
            return new WorkflowStepFilterMetadataDto([]);

        return BuildStepFilterMetadata(
            mappedFieldCodes,
            metadataFieldTypeByCode,
            metadataAllowedValuesByCode);
    }

    public async Task<WorkflowTemplateDetail> GetDetailAsync(ControlNumber templateCtrlNbr, CancellationToken ct = default)
    {
        var template = await workflowTemplateRepository.GetByCtrlNbrAsync(templateCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Workflow template {templateCtrlNbr.Value} not found.");

        return await BuildDetailAsync(template, ct);
    }

    public async Task<List<WorkflowExecutionHistoryDto>> GetExecutionHistoryAsync(
        ControlNumber templateCtrlNbr,
        int take = 100,
        CancellationToken ct = default)
    {
        var template = await workflowTemplateRepository.GetByCtrlNbrAsync(templateCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Workflow template {templateCtrlNbr.Value} not found.");

        var historyRows = await workflowExecutionHistoryRepository.GetByTemplateAsync(template.CtrlNbr, take, ct);

        var triggerTypes = await workflowTriggerTypeRepository.GetAllActiveAsync(ct);
        var effectTypes = await workflowEffectTypeRepository.GetAllActiveAsync(ct);
        var roles = await roleRepository.GetAllAsync(ct);
        var dynamicGroups = await dynamicGroupRepository.GetAllAsync(ct);
        var departments = await departmentRepository.GetAllAsync(ct);
        var crafts = await craftRepository.GetAllAsync(ct);
        var seniorityStates = await seniorityStateRepository.GetAllAsync(ct);

        var triggerTypeNamesByCtrlNbr = triggerTypes.ToDictionary(t => t.CtrlNbr.Value, t => t.Name);
        var effectTypeNamesByCtrlNbr = effectTypes.ToDictionary(t => t.CtrlNbr.Value, t => t.Name);
        var roleNamesByCtrlNbr = roles.ToDictionary(r => r.CtrlNbr.Value, r => r.Name);
        var railroadNamesByCtrlNbr = dynamicGroups.ToDictionary(g => g.CtrlNbr.Value, g => g.Name);
        var departmentNamesByCtrlNbr = departments.ToDictionary(d => d.CtrlNbr.Value, d => d.Name);
        var craftNamesByCtrlNbr = crafts.ToDictionary(c => c.CtrlNbr.Value, c => c.CraftName);
        var seniorityStateNamesByCtrlNbr = seniorityStates.ToDictionary(s => s.CtrlNbr.Value, s => s.StateDescription);

        var aggregateCtrlNbrs = historyRows
            .Where(r => r.AggregateCtrlNbr is not null)
            .Select(r => r.AggregateCtrlNbr!.Value)
            .Distinct()
            .Select(ControlNumber.Create)
            .ToList();

        var aggregateEmployees = aggregateCtrlNbrs.Count == 0
            ? []
            : await employeeRepository.GetByCtrlNbrsAsync(aggregateCtrlNbrs, ct);

        var aggregateDisplayByCtrlNbr = aggregateEmployees
            .GroupBy(e => e.CtrlNbr.Value)
            .ToDictionary(g => g.Key, g => ResolveEmployeeDisplayName(g.First()));

        return historyRows.Select(row =>
        {
            var aggregateDisplay = row.AggregateCtrlNbr is not null
                ? (aggregateDisplayByCtrlNbr.TryGetValue(row.AggregateCtrlNbr.Value, out var display)
                    ? display
                    : row.AggregateCtrlNbr.Value.ToString())
                : string.Empty;

            var detailsDisplayJson = BuildDetailsDisplayJson(
                row.DetailsJson,
                aggregateDisplayByCtrlNbr,
                triggerTypeNamesByCtrlNbr,
                effectTypeNamesByCtrlNbr,
                roleNamesByCtrlNbr,
                railroadNamesByCtrlNbr,
                departmentNamesByCtrlNbr,
                craftNamesByCtrlNbr,
                seniorityStateNamesByCtrlNbr);

            return new WorkflowExecutionHistoryDto(
                row.CtrlNbr,
                row.WorkflowTemplateCtrlNbr,
                row.WorkflowVersionCtrlNbr,
                row.WorkflowVersionNumber,
                row.RailroadCtrlNbr,
                row.TriggerTypeCtrlNbr,
                row.AggregateCtrlNbr,
                aggregateDisplay,
                row.CorrelationId,
                row.StartedAtUtc,
                row.CompletedAtUtc,
                row.Status,
                row.DetailsJson,
                detailsDisplayJson);
        }).ToList();
    }

    private static string BuildDetailsDisplayJson(
        string? detailsJson,
        IReadOnlyDictionary<long, string> aggregateDisplayByCtrlNbr,
        IReadOnlyDictionary<long, string> triggerTypeNamesByCtrlNbr,
        IReadOnlyDictionary<long, string> effectTypeNamesByCtrlNbr,
        IReadOnlyDictionary<long, string> roleNamesByCtrlNbr,
        IReadOnlyDictionary<long, string> railroadNamesByCtrlNbr,
        IReadOnlyDictionary<long, string> departmentNamesByCtrlNbr,
        IReadOnlyDictionary<long, string> craftNamesByCtrlNbr,
        IReadOnlyDictionary<long, string> seniorityStateNamesByCtrlNbr)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
            return string.Empty;

        try
        {
            var node = JsonNode.Parse(detailsJson);
            MapCtrlNbrNodes(node);
            return node?.ToJsonString() ?? detailsJson;
        }
        catch
        {
            return detailsJson;
        }

        void MapCtrlNbrNodes(JsonNode? current)
        {
            if (current is JsonObject obj)
            {
                var properties = obj.ToList();
                foreach (var (propertyName, propertyValue) in properties)
                {
                    MapCtrlNbrNodes(propertyValue);

                    if (!propertyName.EndsWith("CtrlNbr", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!TryGetCtrlNbrValue(propertyValue, out var ctrlNbrValue))
                        continue;

                    var mapped = ResolveMappedName(propertyName, ctrlNbrValue);
                    if (string.IsNullOrWhiteSpace(mapped))
                        continue;

                    obj.Remove(propertyName);
                    obj[GetAliasName(propertyName)] = mapped;
                }

                return;
            }

            if (current is JsonArray array)
            {
                foreach (var child in array)
                    MapCtrlNbrNodes(child);
            }
        }

        string? ResolveMappedName(string propertyName, long ctrlNbrValue)
        {
            if (propertyName.Equals("aggregateCtrlNbr", StringComparison.OrdinalIgnoreCase)
                && aggregateDisplayByCtrlNbr.TryGetValue(ctrlNbrValue, out var aggregateDisplay))
            {
                return aggregateDisplay;
            }

            if (propertyName.Equals("triggerTypeCtrlNbr", StringComparison.OrdinalIgnoreCase)
                && triggerTypeNamesByCtrlNbr.TryGetValue(ctrlNbrValue, out var triggerTypeName))
            {
                return triggerTypeName;
            }

            if (propertyName.Equals("effectTypeCtrlNbr", StringComparison.OrdinalIgnoreCase)
                && effectTypeNamesByCtrlNbr.TryGetValue(ctrlNbrValue, out var effectTypeName))
            {
                return effectTypeName;
            }

            if (propertyName.Equals("roleCtrlNbr", StringComparison.OrdinalIgnoreCase)
                && roleNamesByCtrlNbr.TryGetValue(ctrlNbrValue, out var roleName))
            {
                return roleName;
            }

            if (propertyName.Equals("railroadCtrlNbr", StringComparison.OrdinalIgnoreCase)
                && railroadNamesByCtrlNbr.TryGetValue(ctrlNbrValue, out var railroadName))
            {
                return railroadName;
            }

            if (propertyName.Equals("departmentCtrlNbr", StringComparison.OrdinalIgnoreCase)
                && departmentNamesByCtrlNbr.TryGetValue(ctrlNbrValue, out var departmentName))
            {
                return departmentName;
            }

            if (propertyName.Equals("craftCtrlNbr", StringComparison.OrdinalIgnoreCase)
                && craftNamesByCtrlNbr.TryGetValue(ctrlNbrValue, out var craftName))
            {
                return craftName;
            }

            if (propertyName.Equals("seniorityStateCtrlNbr", StringComparison.OrdinalIgnoreCase)
                && seniorityStateNamesByCtrlNbr.TryGetValue(ctrlNbrValue, out var seniorityStateName))
            {
                return seniorityStateName;
            }

            return null;
        }

        static bool TryGetCtrlNbrValue(JsonNode? valueNode, out long ctrlNbrValue)
        {
            ctrlNbrValue = 0;
            if (valueNode is JsonValue jsonValue)
            {
                if (jsonValue.TryGetValue<long>(out ctrlNbrValue))
                    return ctrlNbrValue > 0;

                if (jsonValue.TryGetValue<string>(out var rawString)
                    && long.TryParse(rawString, out ctrlNbrValue))
                {
                    return ctrlNbrValue > 0;
                }
            }

            var raw = valueNode?.ToString();
            return long.TryParse(raw, out ctrlNbrValue) && ctrlNbrValue > 0;
        }

        static string GetAliasName(string propertyName)
        {
            var suffixIndex = propertyName.LastIndexOf("CtrlNbr", StringComparison.OrdinalIgnoreCase);
            if (suffixIndex <= 0)
                return propertyName;

            var baseName = propertyName[..suffixIndex];
            return string.IsNullOrWhiteSpace(baseName) ? propertyName : baseName;
        }
    }

    private static string ResolveEmployeeDisplayName(Employee employee)
    {
        return !string.IsNullOrWhiteSpace(employee.EmployeeNumber)
            ? employee.EmployeeNumber
            : employee.CtrlNbr.Value.ToString();
    }

    public async Task<WorkflowReferenceCatalogDto> GetReferenceCatalogAsync(ControlNumber railroadCtrlNbr, CancellationToken ct = default)
    {
        var triggerTypes = await workflowTriggerTypeRepository.GetAllActiveAsync(ct);
        var effectTypes = await workflowEffectTypeRepository.GetAllActiveAsync(ct);
        var operatorTypes = await workflowOperatorTypeRepository.GetAllActiveAsync(ct);
        var metadataFieldTypes = await workflowMetadataFieldTypeRepository.GetAllActiveAsync(ct);
        var metadataAllowedValuesByCode = await BuildMetadataAllowedValuesByCodeAsync(railroadCtrlNbr, ct);

        var triggerTypeByCode = triggerTypes.ToDictionary(t => t.Code, StringComparer.Ordinal);
        var metadataFieldTypeByCode = metadataFieldTypes.ToDictionary(t => t.Code, StringComparer.Ordinal);

        var triggerMetadataFieldMaps = new List<WorkflowTriggerMetadataFieldMapDto>();

        foreach (var triggerType in triggerTypes)
            AddMapForTrigger(triggerType.Code, GetDefaultMetadataFieldCodesForTrigger(triggerType.Code));

        return new WorkflowReferenceCatalogDto(
            TriggerTypes: triggerTypes
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => new WorkflowReferenceItemDto(t.CtrlNbr, t.Code, t.Name, []))
                .ToList(),
            EffectTypes: effectTypes
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => new WorkflowReferenceItemDto(t.CtrlNbr, t.Code, t.Name, []))
                .ToList(),
            OperatorTypes: operatorTypes
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => new WorkflowReferenceItemDto(t.CtrlNbr, t.Code, t.Name, []))
                .ToList(),
            MetadataFieldTypes: metadataFieldTypes
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => new WorkflowReferenceItemDto(
                    t.CtrlNbr,
                    t.Code,
                    t.Name,
                    metadataAllowedValuesByCode.TryGetValue(t.Code, out var allowedValues)
                        ? allowedValues
                        : []))
                .ToList(),
            TriggerMetadataFieldMaps: triggerMetadataFieldMaps);

        void AddMapForTrigger(string triggerCode, IReadOnlyList<string> metadataFieldCodes)
        {
            if (!triggerTypeByCode.TryGetValue(triggerCode, out var triggerType))
                return;

            foreach (var fieldCode in metadataFieldCodes)
            {
                if (!metadataFieldTypeByCode.TryGetValue(fieldCode, out var fieldType))
                    continue;

                triggerMetadataFieldMaps.Add(new WorkflowTriggerMetadataFieldMapDto(triggerType.CtrlNbr, fieldType.CtrlNbr));
            }
        }
    }

    private async Task<Dictionary<string, List<string>>> BuildMetadataAllowedValuesByCodeAsync(
        ControlNumber railroadCtrlNbr,
        CancellationToken ct)
    {
        var metadataAllowedValuesByCode = new Dictionary<string, List<string>>(StringComparer.Ordinal)
        {
            [WorkflowMetadataFieldTypeCodes.NotificationType] = GetNotificationTypeValues(),
            [WorkflowMetadataFieldTypeCodes.BoardType] = GetBoardTypeValues()
        };

        var railroad = await dynamicGroupRepository.GetByCtrlNbrAsync(railroadCtrlNbr, ct);
        var parentCtrlNbr = railroad?.ParentCtrlNbr;

        var departments = await departmentRepository.GetByParentAndRailroadAsync(parentCtrlNbr, railroadCtrlNbr);
        metadataAllowedValuesByCode[WorkflowMetadataFieldTypeCodes.DepartmentName] = GetDistinctNonEmptySorted(departments.Select(d => d.Name));

        var crafts = await craftRepository.GetByParentAndRailroadAsync(parentCtrlNbr, railroadCtrlNbr);
        metadataAllowedValuesByCode[WorkflowMetadataFieldTypeCodes.CraftName] = GetDistinctNonEmptySorted(crafts.Select(c => c.CraftName));

        if (parentCtrlNbr is not null)
        {
            var seniorityStates = await seniorityStateRepository.GetByParentCtrlNbrAsync(parentCtrlNbr);
            var stateNames = GetDistinctNonEmptySorted(seniorityStates.Select(s => s.StateDescription));
            metadataAllowedValuesByCode[WorkflowMetadataFieldTypeCodes.SeniorityStateName] = stateNames;
            metadataAllowedValuesByCode[WorkflowMetadataFieldTypeCodes.NewSeniorityState] = stateNames;
        }
        else
        {
            metadataAllowedValuesByCode[WorkflowMetadataFieldTypeCodes.SeniorityStateName] = [];
            metadataAllowedValuesByCode[WorkflowMetadataFieldTypeCodes.NewSeniorityState] = [];
        }

        return metadataAllowedValuesByCode;
    }

    private static List<string> GetNotificationTypeValues()
    {
        return
        [
            NotificationCategories.BoardPlacement,
            NotificationCategories.BulletinAward,
            NotificationCategories.BulletinCancellation,
            NotificationCategories.BulletinLost,
            NotificationCategories.ForceAssign,
            NotificationCategories.GeneralInformation,
            NotificationCategories.PositionChange,
            NotificationCategories.SafetyBulletin,
            NotificationCategories.SeniorityMove,
            NotificationCategories.SeniorityMoveCancelled,
            NotificationCategories.TieUp,
            NotificationCategories.WaitListPromotion,
            NotificationCategories.WorkAreaChange
        ];
    }

    private static List<string> GetBoardTypeValues()
    {
        return Enum.GetNames<BoardType>()
            .Select(NormalizeBoardTypeValue)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeBoardTypeValue(string value)
    {
        return value switch
        {
            nameof(BoardType.ExtendedAbsence) => "Extended Absence",
            nameof(BoardType.ExtraBoard) => "Extra Board",
            nameof(BoardType.NewHire) => "New Hires",
            _ => value
        };
    }

    private static List<string> GetDistinctNonEmptySorted(IEnumerable<string?> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> GetDefaultMetadataFieldCodesForTrigger(string triggerTypeCode)
    {
        return triggerTypeCode switch
        {
            WorkflowTriggerTypeCodes.EmployeeCreated =>
            [
                WorkflowMetadataFieldTypeCodes.DepartmentName,
                WorkflowMetadataFieldTypeCodes.CraftName,
                WorkflowMetadataFieldTypeCodes.SeniorityStateName
            ],
            WorkflowTriggerTypeCodes.SeniorityStatusChanged =>
            [
                WorkflowMetadataFieldTypeCodes.DepartmentName,
                WorkflowMetadataFieldTypeCodes.CraftName,
                WorkflowMetadataFieldTypeCodes.SeniorityStateName,
                WorkflowMetadataFieldTypeCodes.NewSeniorityState
            ],
            WorkflowTriggerTypeCodes.NotificationAccepted =>
            [
                WorkflowMetadataFieldTypeCodes.NotificationType
            ],
            _ => []
        };
    }

    private static WorkflowStepFilterMetadataDto BuildStepFilterMetadata(
        IReadOnlyList<string> fieldCodes,
        IReadOnlyDictionary<string, WorkflowMetadataFieldType> metadataFieldTypeByCode,
        IReadOnlyDictionary<string, List<string>> metadataAllowedValuesByCode)
    {
        var fields = new List<WorkflowMetadataFieldTypeWithValuesDto>(fieldCodes.Count);

        foreach (var fieldCode in fieldCodes.Distinct(StringComparer.Ordinal))
        {
            if (!metadataFieldTypeByCode.TryGetValue(fieldCode, out var fieldType))
                continue;

            fields.Add(new WorkflowMetadataFieldTypeWithValuesDto(
                fieldType.CtrlNbr,
                fieldType.Code,
                fieldType.Name,
                metadataAllowedValuesByCode.TryGetValue(fieldType.Code, out var allowedValues)
                    ? allowedValues
                    : []));
        }

        return new WorkflowStepFilterMetadataDto(fields);
    }

    public async Task<WorkflowTemplateDetail> CreateTemplateAsync(
        ControlNumber railroadCtrlNbr,
        string name,
        bool canStartFromTrigger,
        ControlNumber? triggerTypeCtrlNbr,
        bool isEnabled,
        CancellationToken ct = default)
    {
        triggerTypeCtrlNbr = canStartFromTrigger ? triggerTypeCtrlNbr : null;

        var template = WorkflowTemplate.Create(
            railroadCtrlNbr,
            name.Trim(),
            triggerTypeCtrlNbr,
            isEnabled);

        await workflowTemplateRepository.AddAsync(template, ct);

        var definition = new WorkflowDefinition(
            TriggerTypeCtrlNbr: triggerTypeCtrlNbr,
            TriggerConditionGroupOperator: "ALL",
            TriggerConditions: [],
            Steps: []);

        var initialVersion = WorkflowVersion.Create(
            workflowTemplateCtrlNbr: template.CtrlNbr,
            versionNumber: 1,
            definitionJson: JsonSerializer.Serialize(definition, JsonOptions),
            notes: "Initial draft",
            status: WorkflowVersionStatus.Draft);

        await workflowVersionRepository.AddAsync(initialVersion, ct);

        return await BuildDetailAsync(template, ct);
    }

    public async Task<WorkflowTemplateDetail> SaveDraftAsync(
        ControlNumber templateCtrlNbr,
        WorkflowTemplateUpsertRequest request,
        CancellationToken ct = default)
    {
        return await SaveVersionAsync(templateCtrlNbr, request, WorkflowVersionStatus.Draft, ct);
    }

    public async Task<WorkflowTemplateDetail> PublishAsync(
        ControlNumber templateCtrlNbr,
        WorkflowTemplateUpsertRequest request,
        CancellationToken ct = default)
    {
        if (request.Steps.Count == 0)
            throw new InvalidOperationException("A published workflow must include at least one step.");

        return await SaveVersionAsync(templateCtrlNbr, request, WorkflowVersionStatus.Published, ct);
    }

    public async Task<WorkflowTemplateDetail> RestoreVersionAsDraftAsync(
        ControlNumber templateCtrlNbr,
        int versionNumber,
        string notes,
        CancellationToken ct = default)
    {
        var template = await workflowTemplateRepository.GetByCtrlNbrAsync(templateCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Workflow template {templateCtrlNbr.Value} not found.");

        var versions = await workflowVersionRepository.GetByTemplateAsync(templateCtrlNbr, ct);
        var sourceVersion = versions.FirstOrDefault(v => v.VersionNumber == versionNumber)
            ?? throw new KeyNotFoundException($"Workflow template version v{versionNumber} not found.");

        var latestDraftVersion = versions
            .Where(v => string.Equals(v.Status, WorkflowVersionStatus.Draft, StringComparison.Ordinal))
            .OrderByDescending(v => v.SavedAtUtc)
            .ThenByDescending(v => v.VersionNumber)
            .FirstOrDefault();
        var latestPublishedVersionNumber = versions
            .Where(v => string.Equals(v.Status, WorkflowVersionStatus.Published, StringComparison.Ordinal))
            .Select(v => v.VersionNumber)
            .DefaultIfEmpty(0)
            .Max();

        var draftNotes = string.IsNullOrWhiteSpace(notes)
            ? $"Restored from v{versionNumber}"
            : notes.Trim();

        if (latestDraftVersion is null)
        {
            var restoredVersion = WorkflowVersion.Create(
                workflowTemplateCtrlNbr: template.CtrlNbr,
                versionNumber: latestPublishedVersionNumber + 1,
                definitionJson: sourceVersion.DefinitionJson,
                notes: draftNotes,
                status: WorkflowVersionStatus.Draft);

            await workflowVersionRepository.AddAsync(restoredVersion, ct);
        }
        else
        {
            latestDraftVersion.SaveDraft(sourceVersion.DefinitionJson, draftNotes);
            await workflowVersionRepository.UpdateAsync(latestDraftVersion, ct);
        }

        return await BuildDetailAsync(template, ct);
    }

    public async Task DeleteTemplateAsync(ControlNumber templateCtrlNbr, CancellationToken ct = default)
    {
        var template = await workflowTemplateRepository.GetByCtrlNbrAsync(templateCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Workflow template {templateCtrlNbr.Value} not found.");

        await workflowTemplateRepository.DeleteAsync(templateCtrlNbr, ct);
    }

    public async Task<WorkflowTemplateDetail> SetEnabledAsync(
        ControlNumber templateCtrlNbr,
        bool isEnabled,
        CancellationToken ct = default)
    {
        var template = await workflowTemplateRepository.GetByCtrlNbrAsync(templateCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Workflow template {templateCtrlNbr.Value} not found.");

        template.UpdateDefinition(template.Name, template.TriggerTypeCtrlNbr, isEnabled);
        await workflowTemplateRepository.UpdateAsync(template, ct);

        return await BuildDetailAsync(template, ct);
    }

    private async Task<WorkflowTemplateDetail> SaveVersionAsync(
        ControlNumber templateCtrlNbr,
        WorkflowTemplateUpsertRequest request,
        string status,
        CancellationToken ct)
    {
        request = await NormalizeUpsertRequestAsync(request, ct);

        if (string.Equals(status, WorkflowVersionStatus.Published, StringComparison.Ordinal))
            await ValidatePublishRequestAsync(request, ct);

        var template = await workflowTemplateRepository.GetByCtrlNbrAsync(templateCtrlNbr, ct)
            ?? throw new KeyNotFoundException($"Workflow template {templateCtrlNbr.Value} not found.");

        var triggerTypeCtrlNbr = request.CanStartFromTrigger
            ? request.TriggerTypeCtrlNbr ?? throw new InvalidOperationException("triggerTypeCtrlNbr is required when canStartFromTrigger is true.")
            : null;

        template.UpdateDefinition(request.Name.Trim(), triggerTypeCtrlNbr, request.IsEnabled);
        await workflowTemplateRepository.UpdateAsync(template, ct);

        var versions = await workflowVersionRepository.GetByTemplateAsync(templateCtrlNbr, ct);
        var latestDraftVersion = versions
            .Where(v => string.Equals(v.Status, WorkflowVersionStatus.Draft, StringComparison.Ordinal))
            .OrderByDescending(v => v.SavedAtUtc)
            .ThenByDescending(v => v.VersionNumber)
            .FirstOrDefault();
        var latestPublishedVersionNumber = versions
            .Where(v => string.Equals(v.Status, WorkflowVersionStatus.Published, StringComparison.Ordinal))
            .Select(v => v.VersionNumber)
            .DefaultIfEmpty(0)
            .Max();
        var nextPublishedVersionNumber = latestPublishedVersionNumber + 1;

        var definition = BuildDefinition(
            triggerTypeCtrlNbr,
            request.TriggerConditionGroupOperator,
            request.TriggerConditions,
            request.Steps);

        var definitionJson = JsonSerializer.Serialize(definition, JsonOptions);
        var notes = string.IsNullOrWhiteSpace(request.VersionNotes)
            ? (status == WorkflowVersionStatus.Published ? "Published" : "Draft save")
            : request.VersionNotes.Trim();

        if (string.Equals(status, WorkflowVersionStatus.Draft, StringComparison.Ordinal))
        {
            if (latestDraftVersion is null)
            {
                var draftVersion = WorkflowVersion.Create(
                    workflowTemplateCtrlNbr: templateCtrlNbr,
                    versionNumber: nextPublishedVersionNumber,
                    definitionJson: definitionJson,
                    notes: notes,
                    status: WorkflowVersionStatus.Draft);

                await workflowVersionRepository.AddAsync(draftVersion, ct);
            }
            else
            {
                latestDraftVersion.SaveDraft(definitionJson, notes);
                await workflowVersionRepository.UpdateAsync(latestDraftVersion, ct);
            }
        }
        else
        {
            if (latestDraftVersion is null)
            {
                var publishedVersion = WorkflowVersion.Create(
                    workflowTemplateCtrlNbr: templateCtrlNbr,
                    versionNumber: nextPublishedVersionNumber,
                    definitionJson: definitionJson,
                    notes: notes,
                    status: WorkflowVersionStatus.Published);

                await workflowVersionRepository.AddAsync(publishedVersion, ct);
            }
            else
            {
                latestDraftVersion.SaveDraft(definitionJson, notes);
                latestDraftVersion.Publish();
                await workflowVersionRepository.UpdateAsync(latestDraftVersion, ct);
            }
        }

        return await BuildDetailAsync(template, ct);
    }

    private async Task<WorkflowTemplateDetail> BuildDetailAsync(WorkflowTemplate template, CancellationToken ct)
    {
        var versions = await workflowVersionRepository.GetByTemplateAsync(template.CtrlNbr, ct);
        var orderedVersions = versions.OrderByDescending(v => v.VersionNumber).ToList();
        var latestDraftVersion = orderedVersions
            .Where(v => string.Equals(v.Status, WorkflowVersionStatus.Draft, StringComparison.Ordinal))
            .OrderByDescending(v => v.SavedAtUtc)
            .ThenByDescending(v => v.VersionNumber)
            .FirstOrDefault();
        var latestPublishedVersion = orderedVersions
            .Where(v => string.Equals(v.Status, WorkflowVersionStatus.Published, StringComparison.Ordinal))
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        var currentVersion = latestDraftVersion ?? latestPublishedVersion;

        var currentDefinition = currentVersion is null
            ? new WorkflowDefinition(template.TriggerTypeCtrlNbr, "ALL", [], [])
            : DeserializeDefinition(currentVersion.DefinitionJson);

        var versionRows = orderedVersions
            .Where(v => string.Equals(v.Status, WorkflowVersionStatus.Published, StringComparison.Ordinal))
            .Select(v =>
        {
            var snapshot = DeserializeDefinition(v.DefinitionJson);
            return new WorkflowTemplateVersionDto(
                v.VersionNumber,
                v.Status,
                v.SavedAtUtc,
                v.Notes,
                snapshot.Steps.Count,
                v.DefinitionJson);
        }).ToList();

        return new WorkflowTemplateDetail(
            template.CtrlNbr,
            template.RailroadCtrlNbr,
            template.Name,
            template.TriggerTypeCtrlNbr is not null,
            template.TriggerTypeCtrlNbr,
            template.IsEnabled,
            latestDraftVersion is not null ? WorkflowVersionStatus.Draft : latestPublishedVersion?.Status ?? WorkflowVersionStatus.Draft,
            latestPublishedVersion?.VersionNumber ?? 0,
            currentDefinition.TriggerConditionGroupOperator,
            currentDefinition.TriggerConditions.Select(c => new WorkflowConditionDto(c.CtrlNbr, c.FieldTypeCtrlNbr, c.OperatorTypeCtrlNbr, c.Value)).ToList(),
            MapSteps(currentDefinition.Steps),
            versionRows);
    }

    private static WorkflowDefinition BuildDefinition(
        ControlNumber? triggerTypeCtrlNbr,
        string triggerConditionGroupOperator,
        IReadOnlyList<WorkflowConditionUpsertRequest> triggerConditions,
        IReadOnlyList<WorkflowStepUpsertRequest> steps)
    {
        var definitionSteps = steps
            .OrderBy(s => s.Order)
            .Select(step => new WorkflowStepDefinition(
                CtrlNbr: step.CtrlNbr.Value > 0 ? step.CtrlNbr : ControlNumber.Create(),
                Order: step.Order,
                Name: step.Name,
                IsEnabled: step.IsEnabled,
                FailurePolicy: step.FailurePolicy,
                ConditionGroupOperator: step.ConditionGroupOperator,
                Conditions: step.Conditions.Select(c => new WorkflowConditionDefinition(
                    CtrlNbr: c.CtrlNbr.Value > 0 ? c.CtrlNbr : ControlNumber.Create(),
                    FieldTypeCtrlNbr: c.FieldTypeCtrlNbr,
                    OperatorTypeCtrlNbr: c.OperatorTypeCtrlNbr,
                    Value: c.Value)).ToList(),
                Effects: step.Effects
                    .OrderBy(e => e.Order)
                    .Select(effect => new WorkflowEffectDefinition(
                        CtrlNbr: effect.CtrlNbr.Value > 0 ? effect.CtrlNbr : ControlNumber.Create(),
                        Order: effect.Order,
                        IsEnabled: effect.IsEnabled,
                        EffectTypeCtrlNbr: effect.EffectTypeCtrlNbr,
                        Options: BuildEffectOptions(effect)))
                    .ToList()))
            .ToList();

        return new WorkflowDefinition(
            triggerTypeCtrlNbr,
            string.IsNullOrWhiteSpace(triggerConditionGroupOperator) ? "ALL" : triggerConditionGroupOperator,
            triggerConditions.Select(c => new WorkflowConditionDefinition(
                CtrlNbr: c.CtrlNbr.Value > 0 ? c.CtrlNbr : ControlNumber.Create(),
                FieldTypeCtrlNbr: c.FieldTypeCtrlNbr,
                OperatorTypeCtrlNbr: c.OperatorTypeCtrlNbr,
                Value: c.Value)).ToList(),
            definitionSteps);
    }

    private static Dictionary<string, string> BuildEffectOptions(WorkflowEffectUpsertRequest effect)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(effect.EffectOption))
            options["effectOption"] = effect.EffectOption;

        foreach (var option in effect.Options)
        {
            if (!string.IsNullOrWhiteSpace(option.Key))
                options[option.Key] = option.Value;
        }

        return options;
    }

    private static List<WorkflowStepDto> MapSteps(List<WorkflowStepDefinition> steps)
    {
        return steps
            .OrderBy(s => s.Order)
            .Select(step => new WorkflowStepDto(
                step.CtrlNbr,
                step.Order,
                step.Name,
                step.IsEnabled,
                step.FailurePolicy,
                step.ConditionGroupOperator,
                step.Conditions.Select(c => new WorkflowConditionDto(c.CtrlNbr, c.FieldTypeCtrlNbr, c.OperatorTypeCtrlNbr, c.Value)).ToList(),
                step.Effects
                    .OrderBy(e => e.Order)
                    .Select(effect =>
                    {
                        effect.Options.TryGetValue("effectOption", out var effectOption);
                        if (string.IsNullOrWhiteSpace(effectOption)
                            && effect.Options.TryGetValue(WorkflowOptionKeys.RoleCtrlNbr, out var roleCtrlNbrOption)
                            && !string.IsNullOrWhiteSpace(roleCtrlNbrOption))
                        {
                            effectOption = roleCtrlNbrOption;
                        }

                        return new WorkflowEffectDto(
                            effect.CtrlNbr,
                            effect.Order,
                            effect.IsEnabled,
                            effect.EffectTypeCtrlNbr,
                            effectOption ?? string.Empty,
                            effect.Options.Select(kvp => new WorkflowEffectOptionDto(kvp.Key, kvp.Value)).ToList());
                    })
                    .ToList()))
            .ToList();
    }

    private static WorkflowDefinition DeserializeDefinition(string definitionJson)
    {
        var definition = JsonSerializer.Deserialize<WorkflowDefinition>(definitionJson, JsonOptions);
        if (definition is null)
            throw new InvalidOperationException("Workflow definition could not be deserialized.");

        var normalizedSteps = (definition.Steps ?? [])
            .Select(step => new WorkflowStepDefinition(
                step.CtrlNbr,
                step.Order,
                step.Name,
                step.IsEnabled,
                step.FailurePolicy,
                step.ConditionGroupOperator,
                step.Conditions ?? [],
                step.Effects ?? []))
            .ToList();

        return definition with
        {
            TriggerConditionGroupOperator = string.IsNullOrWhiteSpace(definition.TriggerConditionGroupOperator)
                ? "ALL"
                : definition.TriggerConditionGroupOperator,
            TriggerConditions = definition.TriggerConditions ?? [],
            Steps = normalizedSteps
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private async Task ValidatePublishRequestAsync(WorkflowTemplateUpsertRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Workflow name is required.");

        if (request.CanStartFromTrigger && request.TriggerTypeCtrlNbr is null)
            throw new InvalidOperationException("triggerTypeCtrlNbr is required when canStartFromTrigger is true.");

        if (request.Steps.Count == 0)
            throw new InvalidOperationException("A published workflow must include at least one step.");

        var effectTypes = await workflowEffectTypeRepository.GetAllActiveAsync(ct);
        var effectTypeByCtrlNbr = effectTypes.ToDictionary(e => e.CtrlNbr, e => e.Code);

        var sendInvitationEffectType = await workflowEffectTypeRepository.GetByCodeAsync(WorkflowEffectTypeCodes.SendInvitation, ct);
        var addToRosterBoardEffectType = await workflowEffectTypeRepository.GetByCodeAsync(WorkflowEffectTypeCodes.AddToRosterBoard, ct);
        var createSeniorityMoveEffectType = await workflowEffectTypeRepository.GetByCodeAsync(WorkflowEffectTypeCodes.CreateSeniorityMove, ct);
        var callWorkflowEffectType = await workflowEffectTypeRepository.GetByCodeAsync("Call Workflow", ct);
        var notificationAcceptedTriggerType = await workflowTriggerTypeRepository.GetByCodeAsync(WorkflowTriggerTypeCodes.NotificationAccepted, ct);
        var notificationTypeMetadataFieldType = await workflowMetadataFieldTypeRepository.GetByCodeAsync(WorkflowMetadataFieldTypeCodes.NotificationType, ct);
        var boardTypeMetadataFieldType = await workflowMetadataFieldTypeRepository.GetByCodeAsync(WorkflowMetadataFieldTypeCodes.BoardType, ct);

        var enforceBoardTypeDependency = notificationAcceptedTriggerType is not null
            && notificationTypeMetadataFieldType is not null
            && boardTypeMetadataFieldType is not null
            && request.TriggerTypeCtrlNbr == notificationAcceptedTriggerType.CtrlNbr;

        ValidateConditions(request.TriggerConditions, "trigger");
        var triggerHasBoardPlacementNotificationType = false;
        if (enforceBoardTypeDependency)
        {
            triggerHasBoardPlacementNotificationType = ValidateBoardTypeRequiresBoardPlacementNotificationType(
                request.TriggerConditions,
                "trigger",
                notificationTypeMetadataFieldType!.CtrlNbr,
                boardTypeMetadataFieldType!.CtrlNbr,
                allowByParentScope: false);
        }

        foreach (var step in request.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Name))
                throw new InvalidOperationException("Each workflow step must include a name before publish.");

            if (step.Effects.Count == 0)
                throw new InvalidOperationException("Each workflow step must include at least one effect before publish.");

            ValidateConditions(step.Conditions, $"step '{step.Name}'");
            if (enforceBoardTypeDependency)
            {
                ValidateBoardTypeRequiresBoardPlacementNotificationType(
                    step.Conditions,
                    $"step '{step.Name}'",
                    notificationTypeMetadataFieldType!.CtrlNbr,
                    boardTypeMetadataFieldType!.CtrlNbr,
                    allowByParentScope: triggerHasBoardPlacementNotificationType);
            }

            foreach (var effect in step.Effects)
            {
                if (!effectTypeByCtrlNbr.ContainsKey(effect.EffectTypeCtrlNbr))
                    throw new InvalidOperationException($"Effect type {effect.EffectTypeCtrlNbr.Value} is invalid.");

                if (addToRosterBoardEffectType is not null
                    && effect.EffectTypeCtrlNbr == addToRosterBoardEffectType.CtrlNbr
                    && string.IsNullOrWhiteSpace(effect.EffectOption))
                {
                    throw new InvalidOperationException("Add to Roster Board effect requires a board selection.");
                }

                if (createSeniorityMoveEffectType is not null
                    && effect.EffectTypeCtrlNbr == createSeniorityMoveEffectType.CtrlNbr)
                {
                    var boardType = string.IsNullOrWhiteSpace(effect.EffectOption)
                        ? effect.Options.FirstOrDefault(o => string.Equals(o.Key, WorkflowOptionKeys.BoardType, StringComparison.OrdinalIgnoreCase))?.Value
                        : effect.EffectOption;

                    if (string.IsNullOrWhiteSpace(boardType))
                        throw new InvalidOperationException("Create Seniority Move effect requires a board selection.");

                    var autoMoveDelayHoursRaw = effect.Options.FirstOrDefault(o => string.Equals(o.Key, WorkflowOptionKeys.AutoMoveDelayHours, StringComparison.OrdinalIgnoreCase))?.Value;
                    if (string.IsNullOrWhiteSpace(autoMoveDelayHoursRaw)
                        || !int.TryParse(autoMoveDelayHoursRaw, out var autoMoveDelayHours)
                        || autoMoveDelayHours < 0)
                    {
                        throw new InvalidOperationException("Create Seniority Move effect requires autoMoveDelayHours greater than or equal to 0.");
                    }
                }

                if (callWorkflowEffectType is not null
                    && effect.EffectTypeCtrlNbr == callWorkflowEffectType.CtrlNbr
                    && string.IsNullOrWhiteSpace(effect.EffectOption))
                {
                    throw new InvalidOperationException("Call Workflow effect requires a workflow selection.");
                }

                if (sendInvitationEffectType is not null && effect.EffectTypeCtrlNbr == sendInvitationEffectType.CtrlNbr)
                {
                    var roleCtrlNbrRaw = effect.Options.FirstOrDefault(o => string.Equals(o.Key, WorkflowOptionKeys.RoleCtrlNbr, StringComparison.OrdinalIgnoreCase))?.Value;
                    if (string.IsNullOrWhiteSpace(roleCtrlNbrRaw)
                        || !long.TryParse(roleCtrlNbrRaw, out var roleCtrlNbr)
                        || roleCtrlNbr <= 0)
                    {
                        throw new InvalidOperationException("Send Invitation effect requires a valid role selection.");
                    }

                    var expirationDaysRaw = effect.Options.FirstOrDefault(o => string.Equals(o.Key, WorkflowOptionKeys.ExpirationDays, StringComparison.OrdinalIgnoreCase))?.Value;
                    if (string.IsNullOrWhiteSpace(expirationDaysRaw)
                        || !int.TryParse(expirationDaysRaw, out var expirationDays)
                        || expirationDays is <= 0 or > 90)
                    {
                        throw new InvalidOperationException("Send Invitation effect requires expirationDays between 1 and 90.");
                    }

                    var usePrimaryEmailRaw = effect.Options.FirstOrDefault(o => string.Equals(o.Key, WorkflowOptionKeys.UsePrimaryEmail, StringComparison.OrdinalIgnoreCase))?.Value;
                    if (string.IsNullOrWhiteSpace(usePrimaryEmailRaw) || !bool.TryParse(usePrimaryEmailRaw, out _))
                        throw new InvalidOperationException("Send Invitation effect requires a valid email source option.");
                }
            }
        }

        static void ValidateConditions(List<WorkflowConditionUpsertRequest> conditions, string scope)
        {
            foreach (var condition in conditions)
            {
                var hasField = condition.FieldTypeCtrlNbr.Value > 0;
                var hasOperator = condition.OperatorTypeCtrlNbr.Value > 0;
                var hasValue = !string.IsNullOrWhiteSpace(condition.Value);
                if (hasField != hasOperator || (hasField && !hasValue))
                {
                    throw new InvalidOperationException($"A condition in {scope} is incomplete.");
                }
            }
        }

        static bool ValidateBoardTypeRequiresBoardPlacementNotificationType(
            List<WorkflowConditionUpsertRequest> conditions,
            string scope,
            ControlNumber notificationTypeFieldTypeCtrlNbr,
            ControlNumber boardTypeFieldTypeCtrlNbr,
            bool allowByParentScope)
        {
            var hasBoardTypeCondition = conditions.Any(c => c.FieldTypeCtrlNbr == boardTypeFieldTypeCtrlNbr);
            var hasBoardPlacementNotificationTypeCondition = conditions.Any(c =>
                c.FieldTypeCtrlNbr == notificationTypeFieldTypeCtrlNbr
                && string.Equals(c.Value, NotificationCategories.BoardPlacement, StringComparison.OrdinalIgnoreCase));

            if (!hasBoardTypeCondition)
                return hasBoardPlacementNotificationTypeCondition;

            if (!hasBoardPlacementNotificationTypeCondition && !allowByParentScope)
                throw new InvalidOperationException($"Board Type conditions in {scope} require Notification Type = {NotificationCategories.BoardPlacement} in the same scope or trigger scope.");

            return hasBoardPlacementNotificationTypeCondition;
        }
    }

    private async Task<WorkflowTemplateUpsertRequest> NormalizeUpsertRequestAsync(WorkflowTemplateUpsertRequest request, CancellationToken ct)
    {
        var normalizedRequest = request;

        if (normalizedRequest.CanStartFromTrigger && normalizedRequest.TriggerTypeCtrlNbr is null)
        {
            var defaultTriggerType = (await workflowTriggerTypeRepository.GetAllActiveAsync(ct)).FirstOrDefault();
            if (defaultTriggerType is not null)
                normalizedRequest = normalizedRequest with { TriggerTypeCtrlNbr = defaultTriggerType.CtrlNbr };
        }

        if (normalizedRequest.TriggerConditionGroupOperator is not "ALL" and not "ANY")
            normalizedRequest = normalizedRequest with { TriggerConditionGroupOperator = "ALL" };

        var normalizedTriggerConditions = NormalizeConditions(normalizedRequest.TriggerConditions);

        var normalizedSteps = new List<WorkflowStepUpsertRequest>(normalizedRequest.Steps.Count);
        foreach (var step in normalizedRequest.Steps)
        {
            var normalizedStep = step with
            {
                ConditionGroupOperator = step.ConditionGroupOperator is "ALL" or "ANY" ? step.ConditionGroupOperator : "ALL",
                Conditions = NormalizeConditions(step.Conditions)
            };

            normalizedSteps.Add(normalizedStep);
        }

        normalizedRequest = normalizedRequest with
        {
            TriggerConditions = normalizedTriggerConditions,
            Steps = normalizedSteps
        };

        if (normalizedRequest.Steps.Count == 0)
            return normalizedRequest;

        var sendInvitationEffectType = await workflowEffectTypeRepository.GetByCodeAsync(WorkflowEffectTypeCodes.SendInvitation, ct);
        var createSeniorityMoveEffectType = await workflowEffectTypeRepository.GetByCodeAsync(WorkflowEffectTypeCodes.CreateSeniorityMove, ct);

        if (sendInvitationEffectType is null && createSeniorityMoveEffectType is null)
            return normalizedRequest;

        normalizedSteps = new List<WorkflowStepUpsertRequest>(normalizedRequest.Steps.Count);

        foreach (var step in normalizedRequest.Steps)
        {
            var normalizedEffects = new List<WorkflowEffectUpsertRequest>(step.Effects.Count);
            foreach (var effect in step.Effects)
            {
                if (sendInvitationEffectType is not null
                    && effect.EffectTypeCtrlNbr == sendInvitationEffectType.CtrlNbr)
                {
                    var roleCtrlNbrOption = effect.Options.FirstOrDefault(o => string.Equals(o.Key, WorkflowOptionKeys.RoleCtrlNbr, StringComparison.OrdinalIgnoreCase));
                    if (roleCtrlNbrOption is null || string.IsNullOrWhiteSpace(roleCtrlNbrOption.Value) || !long.TryParse(roleCtrlNbrOption.Value, out var roleCtrlNbrRaw) || roleCtrlNbrRaw <= 0)
                        throw new InvalidOperationException("Send Invitation effect requires a valid roleCtrlNbr option.");

                    var roleCtrlNbr = ControlNumber.Create(roleCtrlNbrRaw);
                    var role = await roleRepository.GetByCtrlNbrAsync(roleCtrlNbr, ct)
                        ?? throw new InvalidOperationException($"Role {roleCtrlNbrRaw} not found for Send Invitation effect.");

                    var normalizedExpirationDays = effect.Options
                        .FirstOrDefault(o => string.Equals(o.Key, WorkflowOptionKeys.ExpirationDays, StringComparison.OrdinalIgnoreCase))?.Value;
                    var expirationDays = 7;
                    if (!string.IsNullOrWhiteSpace(normalizedExpirationDays)
                        && int.TryParse(normalizedExpirationDays, out var parsedExpirationDays)
                        && parsedExpirationDays > 0)
                    {
                        expirationDays = Math.Min(parsedExpirationDays, 90);
                    }

                    var usePrimaryEmail = true;
                    var usePrimaryEmailRaw = effect.Options
                        .FirstOrDefault(o => string.Equals(o.Key, WorkflowOptionKeys.UsePrimaryEmail, StringComparison.OrdinalIgnoreCase))?.Value;
                    if (!string.IsNullOrWhiteSpace(usePrimaryEmailRaw) && bool.TryParse(usePrimaryEmailRaw, out var parsedUsePrimaryEmail))
                        usePrimaryEmail = parsedUsePrimaryEmail;

                    var normalizedOptions = effect.Options
                        .Where(o => !string.Equals(o.Key, WorkflowOptionKeys.RoleCtrlNbr, StringComparison.OrdinalIgnoreCase)
                                    && !string.Equals(o.Key, WorkflowOptionKeys.ExpirationDays, StringComparison.OrdinalIgnoreCase)
                                    && !string.Equals(o.Key, WorkflowOptionKeys.UsePrimaryEmail, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    AddOrReplaceOption(WorkflowOptionKeys.RoleCtrlNbr, role.CtrlNbr.Value.ToString());
                    AddOrReplaceOption(WorkflowOptionKeys.ExpirationDays, expirationDays.ToString());
                    AddOrReplaceOption(WorkflowOptionKeys.UsePrimaryEmail, usePrimaryEmail ? "true" : "false");

                    var normalizedEffect = effect with
                    {
                        Options = normalizedOptions,
                        EffectOption = role.CtrlNbr.Value.ToString()
                    };

                    normalizedEffects.Add(normalizedEffect);
                    continue;

                    void AddOrReplaceOption(string key, string value)
                    {
                        normalizedOptions.RemoveAll(o => string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase));
                        normalizedOptions.Add(new WorkflowEffectOptionDto(key, value));
                    }
                }

                if (createSeniorityMoveEffectType is not null
                    && effect.EffectTypeCtrlNbr == createSeniorityMoveEffectType.CtrlNbr)
                {
                    var boardType = string.IsNullOrWhiteSpace(effect.EffectOption)
                        ? effect.Options.FirstOrDefault(o => string.Equals(o.Key, WorkflowOptionKeys.BoardType, StringComparison.OrdinalIgnoreCase))?.Value
                        : effect.EffectOption;
                    if (string.IsNullOrWhiteSpace(boardType))
                        throw new InvalidOperationException("Create Seniority Move effect requires a valid boardType option.");

                    var autoMoveDelayHoursRaw = effect.Options
                        .FirstOrDefault(o => string.Equals(o.Key, WorkflowOptionKeys.AutoMoveDelayHours, StringComparison.OrdinalIgnoreCase))?.Value;
                    var autoMoveDelayHours = 0;
                    if (!string.IsNullOrWhiteSpace(autoMoveDelayHoursRaw)
                        && int.TryParse(autoMoveDelayHoursRaw, out var parsedDelayHours)
                        && parsedDelayHours >= 0)
                    {
                        autoMoveDelayHours = parsedDelayHours;
                    }

                    var normalizedCreateMoveOptions = effect.Options
                        .Where(o => !string.Equals(o.Key, WorkflowOptionKeys.BoardType, StringComparison.OrdinalIgnoreCase)
                                    && !string.Equals(o.Key, WorkflowOptionKeys.AutoMoveDelayHours, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    AddOrReplaceCreateMoveOption(WorkflowOptionKeys.BoardType, boardType.Trim());
                    AddOrReplaceCreateMoveOption(WorkflowOptionKeys.AutoMoveDelayHours, autoMoveDelayHours.ToString());

                    normalizedEffects.Add(effect with
                    {
                        Options = normalizedCreateMoveOptions,
                        EffectOption = boardType.Trim()
                    });

                    continue;

                    void AddOrReplaceCreateMoveOption(string key, string value)
                    {
                        normalizedCreateMoveOptions.RemoveAll(o => string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase));
                        normalizedCreateMoveOptions.Add(new WorkflowEffectOptionDto(key, value));
                    }
                }

                normalizedEffects.Add(effect);
            }

            normalizedSteps.Add(step with { Effects = normalizedEffects });
        }

        return normalizedRequest with { Steps = normalizedSteps };

        static List<WorkflowConditionUpsertRequest> NormalizeConditions(List<WorkflowConditionUpsertRequest> conditions)
        {
            return conditions
                .Where(c => c.FieldTypeCtrlNbr.Value > 0 || c.OperatorTypeCtrlNbr.Value > 0 || !string.IsNullOrWhiteSpace(c.Value))
                .Select(c => c with
                {
                    Value = c.Value?.Trim() ?? string.Empty
                })
                .ToList();
        }
    }

}

public sealed record WorkflowTemplateSummary(
    ControlNumber CtrlNbr,
    string Name,
    bool CanStartFromTrigger,
    ControlNumber? TriggerTypeCtrlNbr,
    bool IsEnabled,
    string Status,
    int CurrentVersionNumber,
    int StepCount);

public sealed record WorkflowExecutionHistoryDto(
    ControlNumber CtrlNbr,
    ControlNumber WorkflowTemplateCtrlNbr,
    ControlNumber WorkflowVersionCtrlNbr,
    int WorkflowVersionNumber,
    ControlNumber RailroadCtrlNbr,
    ControlNumber? TriggerTypeCtrlNbr,
    ControlNumber? AggregateCtrlNbr,
    string AggregateDisplay,
    string? CorrelationId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string Status,
    string? DetailsJson,
    string DetailsDisplayJson);

public sealed record WorkflowTemplateVersionDto(
    int VersionNumber,
    string Status,
    DateTime SavedAtUtc,
    string Notes,
    int StepCount,
    string DefinitionJson);

public sealed record WorkflowTemplateDetail(
    ControlNumber CtrlNbr,
    ControlNumber RailroadCtrlNbr,
    string Name,
    bool CanStartFromTrigger,
    ControlNumber? TriggerTypeCtrlNbr,
    bool IsEnabled,
    string Status,
    int CurrentVersionNumber,
    string TriggerConditionGroupOperator,
    List<WorkflowConditionDto> TriggerConditions,
    List<WorkflowStepDto> Steps,
    List<WorkflowTemplateVersionDto> Versions);

public sealed record WorkflowStepDto(
    ControlNumber CtrlNbr,
    int Order,
    string Name,
    bool IsEnabled,
    string FailurePolicy,
    string ConditionGroupOperator,
    List<WorkflowConditionDto> Conditions,
    List<WorkflowEffectDto> Effects);

public sealed record WorkflowConditionDto(
    ControlNumber CtrlNbr,
    ControlNumber FieldTypeCtrlNbr,
    ControlNumber OperatorTypeCtrlNbr,
    string Value);

public sealed record WorkflowConditionValueDto(
    ControlNumber FieldTypeCtrlNbr,
    string Value);

public sealed record WorkflowEffectDto(
    ControlNumber CtrlNbr,
    int Order,
    bool IsEnabled,
    ControlNumber EffectTypeCtrlNbr,
    string EffectOption,
    List<WorkflowEffectOptionDto> Options);

public sealed record WorkflowEffectOptionDto(string Key, string Value);

public sealed record WorkflowReferenceItemDto(
    ControlNumber CtrlNbr,
    string Code,
    string Name,
    List<string> AllowedValues);

public sealed record WorkflowTriggerMetadataFieldMapDto(
    ControlNumber TriggerTypeCtrlNbr,
    ControlNumber MetadataFieldTypeCtrlNbr);

public sealed record WorkflowMetadataFieldTypeWithValuesDto(
    ControlNumber CtrlNbr,
    string Code,
    string Name,
    List<string> AllowedValues);

public sealed record WorkflowStepFilterMetadataDto(
    List<WorkflowMetadataFieldTypeWithValuesDto> MetadataFieldTypes);

public sealed record WorkflowReferenceCatalogDto(
    List<WorkflowReferenceItemDto> TriggerTypes,
    List<WorkflowReferenceItemDto> EffectTypes,
    List<WorkflowReferenceItemDto> OperatorTypes,
    List<WorkflowReferenceItemDto> MetadataFieldTypes,
    List<WorkflowTriggerMetadataFieldMapDto> TriggerMetadataFieldMaps);

public sealed record WorkflowTemplateUpsertRequest(
    string Name,
    bool CanStartFromTrigger,
    ControlNumber? TriggerTypeCtrlNbr,
    bool IsEnabled,
    string VersionNotes,
    string TriggerConditionGroupOperator,
    List<WorkflowConditionUpsertRequest> TriggerConditions,
    List<WorkflowStepUpsertRequest> Steps);

public sealed record WorkflowStepUpsertRequest(
    ControlNumber CtrlNbr,
    int Order,
    string Name,
    bool IsEnabled,
    string FailurePolicy,
    string ConditionGroupOperator,
    List<WorkflowConditionUpsertRequest> Conditions,
    List<WorkflowEffectUpsertRequest> Effects);

public sealed record WorkflowConditionUpsertRequest(
    ControlNumber CtrlNbr,
    ControlNumber FieldTypeCtrlNbr,
    ControlNumber OperatorTypeCtrlNbr,
    string Value);

public sealed record WorkflowEffectUpsertRequest(
    ControlNumber CtrlNbr,
    int Order,
    bool IsEnabled,
    ControlNumber EffectTypeCtrlNbr,
    string EffectOption,
    List<WorkflowEffectOptionDto> Options);
