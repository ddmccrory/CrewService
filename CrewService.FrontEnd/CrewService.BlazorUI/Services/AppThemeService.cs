namespace CrewService.BlazorUI.Services;

public class AppThemeService
{
    #region Fields

    private string _theme;
    private string _themeName;
    private string _themeDescription;
    private string _mode;

    private string _previousTheme;
    private string _previousMode;

    #endregion

    #region Properties

    public string Theme => _theme;
    public string ThemeName => _themeName;
    public string ThemeDescription => _themeDescription;
    public string Mode => _mode;

    public string PreviousTheme => _previousTheme;
    public string PreviousMode => _previousMode;

    #endregion

    #region Constructors

    public AppThemeService()
    {
        _theme = "bootstrap/bootstrap.min spacelab.css";
        _themeName = "Spacelab";
        _themeDescription = "Silvery and sleek";
        _mode = "Light";

        _previousTheme = string.Empty;
        _previousMode = string.Empty;
    }

    #endregion

    #region Methods

    public void SetMode(string mode)
    {
        if (string.IsNullOrEmpty(_previousMode))
            _previousMode = _mode;

        if (!string.IsNullOrEmpty(mode))
            _mode = mode;
    }

    public void SetTheme(string themeName)
    {
        if (string.IsNullOrEmpty(_previousTheme))
            _previousTheme = _themeName;

        if (!string.IsNullOrEmpty(themeName))
            _themeName = themeName;

        switch (themeName)
        {
            case "Cerulean":
                {
                    _theme = "bootstrap/bootstrap.min cerulean.css";
                    _themeDescription = "A calm blue sky";
                    break;
                }
            case "Cosmo":
                {
                    _theme = "bootstrap/bootstrap.min cosmo.css";
                    _themeDescription = "An ode to Metro";
                    break;
                }
            case "Cyborg":
                {
                    _theme = "bootstrap/bootstrap.min cyborg.css";
                    _themeDescription = "Jet black and electric blue";
                    break;
                }
            case "Darkly":
                {
                    _theme = "bootstrap/bootstrap.min darkly.css";
                    _themeDescription = "Flatly in night mode";
                    break;
                }
            case "Flatly":
                {
                    _theme = "bootstrap/bootstrap.min flatly.css";
                    _themeDescription = "Flat and modern";
                    break;
                }
            case "Journal":
                {
                    _theme = "bootstrap/bootstrap.min journal.css";
                    _themeDescription = "Crisp like a new sheet of paper";
                    break;
                }
            case "Litera":
                {
                    _theme = "bootstrap/bootstrap.min litera.css";
                    _themeDescription = "The medium is the message";
                    break;
                }
            case "Lumen":
                {
                    _theme = "bootstrap/bootstrap.min lumen.css";
                    _themeDescription = "Light and shadow";
                    break;
                }
            case "Lux":
                {
                    _theme = "bootstrap/bootstrap.min lux.css";
                    _themeDescription = "A touch of class";
                    break;
                }
            case "Materia":
                {
                    _theme = "bootstrap/bootstrap.min materia.css";
                    _themeDescription = "Material is the metaphor";
                    break;
                }
            case "Minty":
                {
                    _theme = "bootstrap/bootstrap.min minty.css";
                    _themeDescription = "A fresh feel";
                    break;
                }
            case "Morph":
                {
                    _theme = "bootstrap/bootstrap.min morph.css";
                    _themeDescription = "A neumorphic layer";
                    break;
                }
            case "Pulse":
                {
                    _theme = "bootstrap/bootstrap.min pulse.css";
                    _themeDescription = "A trace of purple";
                    break;
                }
            case "Quartz":
                {
                    _theme = "bootstrap/bootstrap.min quartz.css";
                    _themeDescription = "A glassmorphic layer";
                    break;
                }
            case "Sandstone":
                {
                    _theme = "bootstrap/bootstrap.min sandstone.css";
                    _themeDescription = "A touch of warmth";
                    break;
                }
            case "Simplex":
                {
                    _theme = "bootstrap/bootstrap.min simplex.css";
                    _themeDescription = "Mini and minimalist";
                    break;
                }
            case "Sketchy":
                {
                    _theme = "bootstrap/bootstrap.min sketchy.css";
                    _themeDescription = "A hand-drawn look for mockups and mirth";
                    break;
                }
            case "Slate":
                {
                    _theme = "bootstrap/bootstrap.min slate.css";
                    _themeDescription = "Shades of gunmetal gray";
                    break;
                }
            case "Solar":
                {
                    _theme = "bootstrap/bootstrap.min solar.css";
                    _themeDescription = "A spin on Solarized";
                    break;
                }
            case "Spacelab":
                {
                    _theme = "bootstrap/bootstrap.min spacelab.css";
                    _themeDescription = "Silvery and sleek";
                    break;
                }
            case "Superhero":
                {
                    _theme = "bootstrap/bootstrap.min superhero.css";
                    _themeDescription = "The brave and the blue";
                    break;
                }
            case "United":
                {
                    _theme = "bootstrap/bootstrap.min united.css";
                    _themeDescription = "Ubuntu orange and unique font";
                    break;
                }
            case "Vapor":
                {
                    _theme = "bootstrap/bootstrap.min vapor.css";
                    _themeDescription = "A cyberpunk aesthetic";
                    break;
                }
            case "Yeti":
                {
                    _theme = "bootstrap/bootstrap.min yeti.css";
                    _themeDescription = "A friendly foundation";
                    break;
                }
            case "Zephyr":
                {
                    _theme = "bootstrap/bootstrap.min zephyr.css";
                    _themeDescription = "Breezy and beautiful";
                    break;
                }
            default:
                {
                    _theme = "bootstrap/bootstrap.min spacelab.css";
                    _themeDescription = "Silvery and sleek";
                    break;
                }
        }
    }

    public void ResetThemeValues()
    {
        SetTheme("Spacelab");

        _mode = "Light";

        ClearPreviousValues();
    }

    public void ClearPreviousValues()
    {
        _previousTheme = string.Empty;
        _previousMode = string.Empty;
    }

    #endregion
}
