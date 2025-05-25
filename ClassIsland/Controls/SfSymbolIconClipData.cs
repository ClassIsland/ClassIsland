using System.Collections.Frozen;
using System.Collections.Generic;
using ClassIsland.Models;

namespace ClassIsland.Controls;

public static class SfSymbolIconClipData
{
    public static readonly FrozenDictionary<SfSymbolIconKind, string> PathData = new Dictionary<SfSymbolIconKind, string>()
    {

        { SfSymbolIconKind.SunMin, "M-6.412109,-6.412109 V35.041016 H35.041016 V-6.412109 H-6.412109 Z"  },

        { SfSymbolIconKind.SunMinFill, "M-6.412109,-6.412109 V35.041016 H35.041016 V-6.412109 H-6.412109 Z"  },

        { SfSymbolIconKind.SunMax, "M-4.785156,-4.785156 V36.667969 H36.667969 V-4.785156 H-4.785156 Z"  },

        { SfSymbolIconKind.SunMaxFill, "M-4.785156,-4.785156 V36.667969 H36.667969 V-4.785156 H-4.785156 Z"  },

        { SfSymbolIconKind.SunMaxTrianglebadgeExclamationmark, "M-4.785156,-5.810547 V35.642578 H36.667969 V-5.810547 H-4.785156 Z"  },

        { SfSymbolIconKind.SunMaxTrianglebadgeExclamationmarkFill, "M-4.785156,-5.87207 V35.581055 H36.667969 V-5.87207 H-4.785156 Z"  },

        { SfSymbolIconKind.Sunrise, "M-3.206055,-6.070312 V35.382813 H38.24707 V-6.070312 H-3.206055 Z"  },

        { SfSymbolIconKind.SunriseFill, "M-3.206055,-6.070312 V35.382813 H38.24707 V-6.070312 H-3.206055 Z"  },

        { SfSymbolIconKind.Sunset, "M-3.206055,-6.070312 V35.382813 H38.24707 V-6.070312 H-3.206055 Z"  },

        { SfSymbolIconKind.SunsetFill, "M-3.206055,-6.070312 V35.382813 H38.24707 V-6.070312 H-3.206055 Z"  },

        { SfSymbolIconKind.SunHorizon, "M-3.206055,-9.255859 V32.197266 H38.24707 V-9.255859 H-3.206055 Z"  },

        { SfSymbolIconKind.SunHorizonFill, "M-3.206055,-9.228516 V32.224609 H38.24707 V-9.228516 H-3.206055 Z"  },

        { SfSymbolIconKind.SunDust, "M-4.949219,-4.238281 V37.214844 H36.503906 V-4.238281 H-4.949219 Z"  },

        { SfSymbolIconKind.SunDustFill, "M-4.949219,-4.238281 V37.214844 H36.503906 V-4.238281 H-4.949219 Z"  },

        { SfSymbolIconKind.SunHaze, "M-4.949219,-4.518555 V36.93457 H36.503906 V-4.518555 H-4.949219 Z" },

        { SfSymbolIconKind.SunHazeFill, "M-4.949219,-4.518555 V36.93457 H36.503906 V-4.518555 H-4.949219 Z" },

        { SfSymbolIconKind.SunRain, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.SunRainFill, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.SunSnow, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.SunSnowFill, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.Moon, "M-6.74707,-6.685547 V34.767578 H34.706055 V-6.685547 H-6.74707 Z" },

        { SfSymbolIconKind.MoonFill, "M-7.287109,-7.225586 V34.227539 H34.166016 V-7.225586 H-7.287109 Z" },

        { SfSymbolIconKind.MoonDust, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.MoonDustFill, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.MoonHaze, "M-4.949219,-5.548836 V35.904289 H36.503906 V-5.548836 H-4.949219 Z" },

        { SfSymbolIconKind.MoonHazeFill, "M-4.949219,-5.548836 V35.904289 H36.503906 V-5.548836 H-4.949219 Z" },

        { SfSymbolIconKind.Sparkles, "M-7.533203,-4.791992 V36.661133 H33.919922 V-4.791992 H-7.533203 Z" },

        { SfSymbolIconKind.MoonStars, "M-6.077148,-5.222656 V36.230469 H35.375977 V-5.222656 H-6.077148 Z" },

        { SfSymbolIconKind.MoonStarsFill, "M-6.077148,-5.222656 V36.230469 H35.375977 V-5.222656 H-6.077148 Z" },

        { SfSymbolIconKind.Cloud, "M-3.636719,-9.283203 V32.169922 H37.816406 V-9.283203 H-3.636719 Z" },

        { SfSymbolIconKind.CloudFill, "M-3.636719,-9.283203 V32.169922 H37.816406 V-9.283203 H-3.636719 Z" },

        { SfSymbolIconKind.CloudDrizzle, "M-4.504883,-4.177892 V37.275233 H36.948242 V-4.177892 H-4.504883 Z" },

        { SfSymbolIconKind.CloudDrizzleFill, "M-4.504883,-4.177892 V37.275233 H36.948242 V-4.177892 H-4.504883 Z" },

        { SfSymbolIconKind.CloudRain, "M-4.504883,-4.157161 V37.295964 H36.948242 V-4.157161 H-4.504883 Z" },

        { SfSymbolIconKind.CloudRainFill, "M-4.504883,-4.157161 V37.295964 H36.948242 V-4.157161 H-4.504883 Z" },

        { SfSymbolIconKind.CloudHeavyrain, "M-4.504883,-4.15498 V37.298145 H36.948242 V-4.15498 H-4.504883 Z" },

        { SfSymbolIconKind.CloudHeavyrainFill, "M-4.504883,-4.15498 V37.298145 H36.948242 V-4.15498 H-4.504883 Z" },

        { SfSymbolIconKind.CloudFog, "M-4.504883,-4.874023 V36.579102 H36.948242 V-4.874023 H-4.504883 Z" },

        { SfSymbolIconKind.CloudFogFill, "M-4.504883,-4.874023 V36.579102 H36.948242 V-4.874023 H-4.504883 Z" },

        { SfSymbolIconKind.CloudHail, "M-4.504883,-4.067383 V37.385742 H36.948242 V-4.067383 H-4.504883 Z" },

        { SfSymbolIconKind.CloudHailFill, "M-4.504883,-4.067383 V37.385742 H36.948242 V-4.067383 H-4.504883 Z" },

        { SfSymbolIconKind.CloudSnow, "M-4.504883,-3.602539 V37.850586 H36.948242 V-3.602539 H-4.504883 Z" },

        { SfSymbolIconKind.CloudSnowFill, "M-4.504883,-3.602539 V37.850586 H36.948242 V-3.602539 H-4.504883 Z" },

        { SfSymbolIconKind.CloudSleet, "M-4.504883,-3.267578 V38.185547 H36.948242 V-3.267578 H-4.504883 Z" },

        { SfSymbolIconKind.CloudSleetFill, "M-4.504883,-3.267578 V38.185547 H36.948242 V-3.267578 H-4.504883 Z" },

        { SfSymbolIconKind.CloudBolt, "M-4.504883,-3.614449 V37.838676 H36.948242 V-3.614449 H-4.504883 Z" },

        { SfSymbolIconKind.CloudBoltFill, "M-4.504883,-3.614449 V37.838676 H36.948242 V-3.614449 H-4.504883 Z" },

        { SfSymbolIconKind.CloudBoltRain, "M-4.504883,-3.614449 V37.838676 H36.948242 V-3.614449 H-4.504883 Z" },

        { SfSymbolIconKind.CloudBoltRainFill, "M-4.504883,-3.614449 V37.838676 H36.948242 V-3.614449 H-4.504883 Z" },

        { SfSymbolIconKind.CloudSun, "M-0.0,-6.09082 V35.362305 H41.453125 V-6.09082 H-0.0 Z" },

        { SfSymbolIconKind.CloudSunFill, "M-0.03418,-6.207031 V35.246094 H41.418945 V-6.207031 H-0.03418 Z" },

        { SfSymbolIconKind.CloudSunRain, "M-0.0,-0.631442 V40.821683 H41.453125 V-0.631442 H-0.0 Z" },

        { SfSymbolIconKind.CloudSunRainFill, "M-0.047852,-0.631442 V40.821683 H41.405273 V-0.631442 H-0.047852 Z" },

        { SfSymbolIconKind.CloudSunBolt, "M-0.0,-0.134957 V41.318168 H41.453125 V-0.134957 H-0.0 Z" },

        { SfSymbolIconKind.CloudSunBoltFill, "M-0.047852,-0.131991 V41.321134 H41.405273 V-0.131991 H-0.047852 Z" },

        { SfSymbolIconKind.CloudMoon, "M-1.82784,-7.985145 V33.46798 H39.625285 V-7.985145 H-1.82784 Z" },

        { SfSymbolIconKind.CloudMoonFill, "M-1.982422,-8.387481 V33.065644 H39.470703 V-8.387481 H-1.982422 Z" },

        { SfSymbolIconKind.CloudMoonRain, "M-1.82784,-2.533991 V38.919134 H39.625285 V-2.533991 H-1.82784 Z" },

        { SfSymbolIconKind.CloudMoonRainFill, "M-1.948242,-2.837824 V38.615301 H39.504883 V-2.837824 H-1.948242 Z" },

        { SfSymbolIconKind.CloudMoonBolt, "M-1.82784,-2.035786 V39.417339 H39.625285 V-2.035786 H-1.82784 Z" },

        { SfSymbolIconKind.CloudMoonBoltFill, "M-1.948242,-2.332181 V39.120944 H39.504883 V-2.332181 H-1.948242 Z" },

        { SfSymbolIconKind.Smoke, "M-3.677734,-7.998047 V33.455078 H37.775391 V-7.998047 H-3.677734 Z" },

        { SfSymbolIconKind.SmokeFill, "M-3.629883,-8.039062 V33.414063 H37.823242 V-8.039062 H-3.629883 Z" },

        { SfSymbolIconKind.Wind, "M-6.262844,-7.916016 V33.537109 H35.190281 V-7.916016 H-6.262844 Z" },

        { SfSymbolIconKind.WindSnow, "M-6.262844,-6.706055 V34.74707 H35.190281 V-6.706055 H-6.262844 Z" },

        { SfSymbolIconKind.Snowflake, "M-8.10513,-6.371094 V35.082031 H33.347995 V-6.371094 H-8.10513 Z" },

        { SfSymbolIconKind.SnowflakeSlash, "M-6.662466,-6.371094 V35.082031 H34.790659 V-6.371094 H-6.662466 Z" },

        { SfSymbolIconKind.Tornado, "M-6.890625,-4.475966 V36.977159 H34.5625 V-4.475966 H-6.890625 Z" },

        { SfSymbolIconKind.Tropicalstorm, "M-11.422852,-5.427734 V36.025391 H30.030273 V-5.427734 H-11.422852 Z" },

        { SfSymbolIconKind.Hurricane, "M-11.422852,-5.427734 V36.025391 H30.030273 V-5.427734 H-11.422852 Z" },

        { SfSymbolIconKind.ThermometerSun, "M-5.817383,-2.365234 V39.087891 H35.635742 V-2.365234 H-5.817383 Z" },

        { SfSymbolIconKind.ThermometerSunFill, "M-5.817383,-2.365234 V39.087891 H35.635742 V-2.365234 H-5.817383 Z" },

        { SfSymbolIconKind.ThermometerSnowflake, "M-7.248825,-4.791992 V36.661133 H34.2043 V-4.791992 H-7.248825 Z" },

        { SfSymbolIconKind.ThermometerVariable, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.ThermometerVariableAndFigure, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.ThermometerLow, "M-10.773438,-4.949219 V36.503906 H30.679687 V-4.949219 H-10.773438 Z" },

        { SfSymbolIconKind.ThermometerMedium, "M-10.773438,-4.949219 V36.503906 H30.679687 V-4.949219 H-10.773438 Z" },

        { SfSymbolIconKind.ThermometerHigh, "M-10.773438,-4.949219 V36.503906 H30.679687 V-4.949219 H-10.773438 Z" },

        { SfSymbolIconKind.ThermometerMediumSlash, "M-8.266338,-4.949219 V36.503906 H33.186787 V-4.949219 H-8.266338 Z" },

        { SfSymbolIconKind.DegreesignFahrenheit, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.DegreesignCelsius, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.AqiLow, "M-5.94043,-3.739258 V37.713867 H35.512695 V-3.739258 H-5.94043 Z" },

        { SfSymbolIconKind.AqiMedium, "M-5.564453,-3.363281 V38.089844 H35.888672 V-3.363281 H-5.564453 Z" },

        { SfSymbolIconKind.AqiHigh, "M-2.290039,-3.007812 V38.445313 H39.163086 V-3.007812 H-2.290039 Z" },

        { SfSymbolIconKind.Humidity, "M-5.635896,-8.401367 V33.051758 H35.817229 V-8.401367 H-5.635896 Z" },

        { SfSymbolIconKind.HumidityFill, "M-5.642732,-8.408203 V33.044922 H35.810393 V-8.408203 H-5.642732 Z" },

        { SfSymbolIconKind.Rainbow, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.CloudRainbowCrop, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.CloudRainbowCropFill, "M-8.09375,-4.019531 V37.433594 H33.359375 V-4.019531 H-8.09375 Z" },

        { SfSymbolIconKind.CarbonMonoxideCloud, "M-3.46582,-6.958984 V34.494141 H37.987305 V-6.958984 H-3.46582 Z" },

        { SfSymbolIconKind.CarbonMonoxideCloudFill, "M-3.46582,-6.958984 V34.494141 H37.987305 V-6.958984 H-3.46582 Z" },

        { SfSymbolIconKind.CarbonDioxideCloud, "M-3.46582,-6.958984 V34.494141 H37.987305 V-6.958984 H-3.46582 Z" },

        { SfSymbolIconKind.CarbonDioxideCloudFill, "M-3.46582,-6.958984 V34.494141 H37.987305 V-6.958984 H-3.46582 Z" },

    }.ToFrozenDictionary();
}