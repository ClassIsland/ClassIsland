using System.Collections.Frozen;
using System.Collections.Generic;
using ClassIsland.Models;

namespace ClassIsland.Controls;

public static class SfSymbolIconClipData
{
    public static FrozenDictionary<SfSymbolIconKind, string> PathData = new Dictionary<SfSymbolIconKind, string>()
    {

        { SfSymbolIconKind.SunMin, "M0,0 V28.62890625 H28.62890625 V0 H0 Z" },

        { SfSymbolIconKind.SunMinFill, "M0,0 V28.62890625 H28.62890625 V0 H0 Z" },

        { SfSymbolIconKind.SunMax, "M0,0 V31.8828125 H31.8828125 V0 H0 Z" },

        { SfSymbolIconKind.SunMaxFill, "M0,0 V31.8828125 H31.8828125 V0 H0 Z" },

        { SfSymbolIconKind.SunMaxTrianglebadgeExclamationmark, "M0,0 V29.83203125 H31.8828125 V0 H0 Z" },

        { SfSymbolIconKind.SunMaxTrianglebadgeExclamationmarkFill, "M0,0 V29.708984375 H31.8828125 V0 H0 Z" },

        { SfSymbolIconKind.Sunrise, "M0,0 V29.3125 H35.041015625 V0 H0 Z" },

        { SfSymbolIconKind.SunriseFill, "M0,0 V29.3125 H35.041015625 V0 H0 Z" },

        { SfSymbolIconKind.Sunset, "M0,0 V29.3125 H35.041015625 V0 H0 Z" },

        { SfSymbolIconKind.SunsetFill, "M0,0 V29.3125 H35.041015625 V0 H0 Z" },

        { SfSymbolIconKind.SunHorizon, "M0,0 V22.94140625 H35.041015625 V0 H0 Z" },

        { SfSymbolIconKind.SunHorizonFill, "M0,0 V22.99609375 H35.041015625 V0 H0 Z" },

        { SfSymbolIconKind.SunDust, "M0,0 V32.9765625 H31.5546875 V0 H0 Z" },

        { SfSymbolIconKind.SunDustFill, "M0,0 V32.9765625 H31.5546875 V0 H0 Z" },

        { SfSymbolIconKind.SunHaze, "M0,0 V32.416015625 H31.5546875 V0 H0 Z" },

        { SfSymbolIconKind.SunHazeFill, "M0,0 V32.416015625 H31.5546875 V0 H0 Z" },

        { SfSymbolIconKind.SunRain, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.SunRainFill, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.SunSnow, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.SunSnowFill, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.Moon, "M0,0 V28.08203125 H27.958984375 V0 H0 Z" },

        { SfSymbolIconKind.MoonFill, "M0,0 V27.001953125 H26.87890625 V0 H0 Z" },

        { SfSymbolIconKind.MoonDust, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.MoonDustFill, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.MoonHaze, "M0,0 V30.355453757111 H31.5546875 V0 H0 Z" },

        { SfSymbolIconKind.MoonHazeFill, "M0,0 V30.355453757111 H31.5546875 V0 H0 Z" },

        { SfSymbolIconKind.Sparkles, "M0,0 V31.869140625 H26.38671875 V0 H0 Z" },

        { SfSymbolIconKind.MoonStars, "M0,0 V31.0078125 H29.298828125 V0 H0 Z" },

        { SfSymbolIconKind.MoonStarsFill, "M0,0 V31.0078125 H29.298828125 V0 H0 Z" },

        { SfSymbolIconKind.Cloud, "M0,0 V22.88671875 H34.1796875 V0 H0 Z" },

        { SfSymbolIconKind.CloudFill, "M0,0 V22.88671875 H34.1796875 V0 H0 Z" },

        { SfSymbolIconKind.CloudDrizzle, "M0,0 V33.0973411985618 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudDrizzleFill, "M0,0 V33.0973411985618 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudRain, "M0,0 V33.1388036293324 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudRainFill, "M0,0 V33.1388036293324 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudHeavyrain, "M0,0 V33.1431652422053 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudHeavyrainFill, "M0,0 V33.1431652422053 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudFog, "M0,0 V31.705078125 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudFogFill, "M0,0 V31.705078125 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudHail, "M0,0 V33.318359375 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudHailFill, "M0,0 V33.318359375 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudSnow, "M0,0 V34.248046875 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudSnowFill, "M0,0 V34.248046875 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudSleet, "M0,0 V34.91796875 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudSleetFill, "M0,0 V34.91796875 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudBolt, "M0,0 V34.2242274158563 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudBoltFill, "M0,0 V34.2242274158563 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudBoltRain, "M0,0 V34.2242274158563 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudBoltRainFill, "M0,0 V34.2242274158563 H32.443359375 V0 H0 Z" },

        { SfSymbolIconKind.CloudSun, "M0,0 V29.271484375 H41.453125 V0 H0 Z" },

        { SfSymbolIconKind.CloudSunFill, "M0,0 V29.0390625 H41.384765625 V0 H0 Z" },

        { SfSymbolIconKind.CloudSunRain, "M0,0 V40.1902402449358 H41.453125 V0 H0 Z" },

        { SfSymbolIconKind.CloudSunRainFill, "M0,0 V40.1902402449358 H41.357421875 V0 H0 Z" },

        { SfSymbolIconKind.CloudSunBolt, "M0,0 V41.1832117908563 H41.453125 V0 H0 Z" },

        { SfSymbolIconKind.CloudSunBoltFill, "M0,0 V41.1891436286057 H41.357421875 V0 H0 Z" },

        { SfSymbolIconKind.CloudMoon, "M0,0 V25.4828344798734 H37.7974450776796 V0 H0 Z" },

        { SfSymbolIconKind.CloudMoonFill, "M0,0 V24.6781633678055 H37.48828125 V0 H0 Z" },

        { SfSymbolIconKind.CloudMoonRain, "M0,0 V36.3851434287699 H37.7974450776796 V0 H0 Z" },

        { SfSymbolIconKind.CloudMoonRainFill, "M0,0 V35.777476220603 H37.556640625 V0 H0 Z" },

        { SfSymbolIconKind.CloudMoonBolt, "M0,0 V37.3815522302081 H37.7974450776796 V0 H0 Z" },

        { SfSymbolIconKind.CloudMoonBoltFill, "M0,0 V36.7887633670838 H37.556640625 V0 H0 Z" },

        { SfSymbolIconKind.Smoke, "M0,0 V25.45703125 H34.09765625 V0 H0 Z" },

        { SfSymbolIconKind.SmokeFill, "M0,0 V25.375 H34.193359375 V0 H0 Z" },

        { SfSymbolIconKind.Wind, "M0,0 V25.62109375 H28.9274368495906 V0 H0 Z" },

        { SfSymbolIconKind.WindSnow, "M0,0 V28.041015625 H28.9274368495906 V0 H0 Z" },

        { SfSymbolIconKind.Snowflake, "M0,0 V28.7109375 H25.242865673531 V0 H0 Z" },

        { SfSymbolIconKind.SnowflakeSlash, "M0,0 V28.7109375 H28.1281935448425 V0 H0 Z" },

        { SfSymbolIconKind.Tornado, "M0,0 V32.5011926471653 H27.671875 V0 H0 Z" },

        { SfSymbolIconKind.Tropicalstorm, "M0,0 V30.59765625 H18.607421875 V0 H0 Z" },

        { SfSymbolIconKind.Hurricane, "M0,0 V30.59765625 H18.607421875 V0 H0 Z" },

        { SfSymbolIconKind.ThermometerSun, "M0,0 V36.72265625 H29.818359375 V0 H0 Z" },

        { SfSymbolIconKind.ThermometerSunFill, "M0,0 V36.72265625 H29.818359375 V0 H0 Z" },

        { SfSymbolIconKind.ThermometerSnowflake, "M0,0 V31.869140625 H26.9554740765955 V0 H0 Z" },

        { SfSymbolIconKind.ThermometerVariable, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.ThermometerVariableAndFigure, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.ThermometerLow, "M0,0 V31.5546875 H19.90625 V0 H0 Z" },

        { SfSymbolIconKind.ThermometerMedium, "M0,0 V31.5546875 H19.90625 V0 H0 Z" },

        { SfSymbolIconKind.ThermometerHigh, "M0,0 V31.5546875 H19.90625 V0 H0 Z" },

        { SfSymbolIconKind.ThermometerMediumSlash, "M0,0 V31.5546875 H24.920449589685 V0 H0 Z" },

        { SfSymbolIconKind.DegreesignFahrenheit, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.DegreesignCelsius, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.AqiLow, "M0,0 V33.974609375 H29.572265625 V0 H0 Z" },

        { SfSymbolIconKind.AqiMedium, "M0,0 V34.7265625 H30.32421875 V0 H0 Z" },

        { SfSymbolIconKind.AqiHigh, "M0,0 V35.4375 H36.873046875 V0 H0 Z" },

        { SfSymbolIconKind.Humidity, "M0,0 V24.650390625 H30.1813327899873 V0 H0 Z" },

        { SfSymbolIconKind.HumidityFill, "M0,0 V24.63671875 H30.1676609149873 V0 H0 Z" },

        { SfSymbolIconKind.Rainbow, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.CloudRainbowCrop, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.CloudRainbowCropFill, "M0,0 V33.4140625 H25.265625 V0 H0 Z" },

        { SfSymbolIconKind.CarbonMonoxideCloud, "M0,0 V27.53515625 H34.521484375 V0 H0 Z" },

        { SfSymbolIconKind.CarbonMonoxideCloudFill, "M0,0 V27.53515625 H34.521484375 V0 H0 Z" },

        { SfSymbolIconKind.CarbonDioxideCloud, "M0,0 V27.53515625 H34.521484375 V0 H0 Z" },

        { SfSymbolIconKind.CarbonDioxideCloudFill, "M0,0 V27.53515625 H34.521484375 V0 H0 Z" },

    }.ToFrozenDictionary();
}