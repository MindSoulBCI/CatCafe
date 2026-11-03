using UnityEngine;

namespace SpatialCatCafe
{
    // Synthesized feedback; no external recordings or network dependency.
    public sealed class CafeAudio : MonoBehaviour
    {
        CafeSession session;
        AudioSource source;
        AudioClip purr, chime;
        public void Configure(CafeSession value)
        {
            session = value; source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0; source.volume = .16f; source.playOnAwake = false;
            purr = Tone("Soft purr", .65f, true); chime = Tone("Cafe chime", .4f, false);
            session.Acted += Play;
        }
        AudioClip Tone(string name, float duration, bool low)
        {
            const int rate = 22050; int count = Mathf.RoundToInt(rate * duration); var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Sin(Mathf.PI * i / count) * Mathf.Exp(-t * 3);
                samples[i] = low
                    ? Mathf.Sin(t * Mathf.PI * 2 * 135) * (.6f + .4f * Mathf.Sin(t * 2 * Mathf.PI * 26)) * envelope
                    : (Mathf.Sin(t * 2 * Mathf.PI * 660) + .3f * Mathf.Sin(t * 2 * Mathf.PI * 990)) * envelope * .5f;
            }
            var clip = AudioClip.Create(name, count, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        void Play(CatAction action, int index)
        { if (session.State.sound) source.PlayOneShot(action == CatAction.Pet || action == CatAction.Rest ? purr : chime); }
        void OnDestroy()
        {
            if (session) session.Acted -= Play;
            if (purr) Destroy(purr); if (chime) Destroy(chime);
        }
    }
}
