using NAudio.CoreAudioApi;

namespace AiVoiceTest.Infrastructure.Audio;

public static class AudioInputDeviceLister
{
    public static IReadOnlyList<AudioCaptureDeviceInfo> ListActiveCaptureDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
        var defaultId = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console).ID;

        return devices
            .Select((device, index) => new AudioCaptureDeviceInfo(
                index,
                device.FriendlyName,
                device.ID == defaultId))
            .ToList();
    }
}

public sealed record AudioCaptureDeviceInfo(int Index, string Name, bool IsDefault);
