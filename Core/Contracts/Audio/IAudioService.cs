namespace Maple.Core
{
    public interface IAudioService
    {
        void PlayBGM(string key, float fadeDuration = 1f);
        void StopBGM(float fadeDuration = 1f);
        void PauseBGM();
        void ResumeBGM();
        void PlaySFX(string key);
        float BGMVolume { get; set; }
        float SFXVolume { get; set; }
    }
}
