using System;
using System.IO;
using System.Xml.Linq;

namespace BloodyMaryLauncher.Services;

public static class GraphicsResetService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CitizenFX", "rdr3_settings", "system.xml");

    private const string GraphicsXml = @"<graphics>
    <tessellation>kSettingLevel_Low</tessellation>
    <shadowQuality>kSettingLevel_Low</shadowQuality>
    <farShadowQuality>kSettingLevel_Low</farShadowQuality>
    <reflectionQuality>kSettingLevel_Low</reflectionQuality>
    <mirrorQuality>kSettingLevel_Low</mirrorQuality>
    <ssao>kSettingLevel_Medium</ssao>
    <textureQuality>kSettingLevel_Ultra</textureQuality>
    <particleQuality>kSettingLevel_Low</particleQuality>
    <waterQuality>kSettingLevel_Low</waterQuality>
    <volumetricsQuality>kSettingLevel_Low</volumetricsQuality>
    <lightingQuality>kSettingLevel_Low</lightingQuality>
    <ambientLightingQuality>kSettingLevel_Low</ambientLightingQuality>
    <anisotropicFiltering value=""0"" />
    <dlssIndex value=""0"" />
    <dlssQuality value=""5"" />
    <dlssSharpen value=""0.350000"" />
    <fsr2Index value=""0"" />
    <fsr2Sharpen value=""0.350000"" />
    <taa>kSettingLevel_Medium</taa>
    <fxaaEnabled value=""false"" />
    <msaa value=""0"" />
    <graphicsQualityPreset value=""0.000000"" />
    <hdr value=""false"" />
    <hdr10PlusGaming value=""false"" />
    <hdrIntensity value=""100"" />
    <hdrPeakBrightness value=""1000"" />
    <hdrFilmicMode value=""true"" />
    <gamma value=""15"" />
    <hdrSettingsMigrated value=""false"" />
  </graphics>";

    private const string AdvancedGraphicsXml = @"<advancedGraphics>
    <API>kSettingAPI_DX12</API>
    <locked value=""true"" />
    <asyncComputeEnabled value=""false"" />
    <transferQueuesEnabled value=""false"" />
    <shadowSoftShadows>kSettingLevel_Low</shadowSoftShadows>
    <motionBlur value=""false"" />
    <motionBlurLimit value=""16.000000"" />
    <particleLightingQuality>kSettingLevel_Low</particleLightingQuality>
    <waterReflectionSSR value=""true"" />
    <waterRefractionQuality>kSettingLevel_Low</waterRefractionQuality>
    <waterReflectionQuality>kSettingLevel_Low</waterReflectionQuality>
    <waterSimulationQuality value=""1"" />
    <waterLightingQuality>kSettingLevel_Medium</waterLightingQuality>
    <furDisplayQuality>kSettingLevel_Medium</furDisplayQuality>
    <maxTexUpgradesPerFrame value=""5"" />
    <shadowGrassShadows>kSettingLevel_Low</shadowGrassShadows>
    <shadowParticleShadows value=""false"" />
    <shadowLongShadows value=""false"" />
    <directionalShadowsAlpha value=""false"" />
    <worldHeightShadowQuality value=""0.000000"" />
    <directionalScreenSpaceShadowQuality value=""0.000000"" />
    <ambientMaskVolumesHighPrecision value=""false"" />
    <scatteringVolumeQuality>kSettingLevel_Low</scatteringVolumeQuality>
    <volumetricsRaymarchQuality>kSettingLevel_Low</volumetricsRaymarchQuality>
    <volumetricsLightingQuality>kSettingLevel_Low</volumetricsLightingQuality>
    <volumetricsRaymarchResolutionUnclamped value=""false"" />
    <terrainShadowQuality>kSettingLevel_Low</terrainShadowQuality>
    <damageModelsDisabled value=""true"" />
    <decalQuality>kSettingLevel_Low</decalQuality>
    <ssaoFullScreenEnabled value=""false"" />
    <ssaoType value=""0"" />
    <ssdoSampleCount value=""4"" />
    <ssdoUseDualRadii value=""false"" />
    <ssdoResolution>kSettingLevel_Low</ssdoResolution>
    <ssdoTAABlendEnabled value=""true"" />
    <ssroSampleCount value=""2"" />
    <snowGlints value=""true"" />
    <POMQuality>kSettingLevel_Low</POMQuality>
    <probeRelightEveryFrame value=""false"" />
    <scalingMode>kSettingScale_Mode1o1</scalingMode>
    <reflectionMSAA value=""0"" />
    <lodScale value=""0.750000"" />
    <grassLod value=""0.500000"" />
    <pedLodBias value=""0.000000"" />
    <vehicleLodBias value=""0.000000"" />
    <sharpenIntensity value=""0.000000"" />
    <treeQuality>kSettingLevel_Low</treeQuality>
    <deepsurfaceQuality>kSettingLevel_Low</deepsurfaceQuality>
    <treeTessellationEnabled value=""false"" />
  </advancedGraphics>";

    public static (bool success, string message) ResetGraphics()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return (false, $"Einstellungsdatei nicht gefunden:\n{SettingsPath}");

            // Backup erstellen
            var backupPath = SettingsPath + ".backup";
            File.Copy(SettingsPath, backupPath, overwrite: true);

            var doc = XDocument.Load(SettingsPath);
            var root = doc.Root;
            if (root == null)
                return (false, "Ungültige XML-Datei.");

            // Replace <graphics> section
            var graphicsElement = root.Element("graphics");
            var newGraphics = XElement.Parse(GraphicsXml);
            if (graphicsElement != null)
                graphicsElement.ReplaceWith(newGraphics);
            else
                root.Add(newGraphics);

            // Replace <advancedGraphics> section
            var advGraphicsElement = root.Element("advancedGraphics");
            var newAdvGraphics = XElement.Parse(AdvancedGraphicsXml);
            if (advGraphicsElement != null)
                advGraphicsElement.ReplaceWith(newAdvGraphics);
            else
                root.Add(newAdvGraphics);

            doc.Save(SettingsPath);
            return (true, $"Grafikeinstellungen zurückgesetzt. Backup: system.xml.backup");
        }
        catch (Exception ex)
        {
            return (false, $"Fehler beim Zurücksetzen der Grafik: {ex.Message}");
        }
    }
}
