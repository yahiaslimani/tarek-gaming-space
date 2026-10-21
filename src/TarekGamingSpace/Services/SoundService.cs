using System;
using System.Collections.Generic;
using System.IO;
using System.Media;

namespace TarekGamingSpace.Services
{
    /// <summary>
    /// The three built-in alert sounds are synthesized at startup, so there are no audio files to ship.
    /// The alert loops until every ended poste has been clicked.
    /// </summary>
    public sealed class SoundService : IDisposable
    {
        const int Rate = 22050;

        readonly Dictionary<string, byte[]> _wavs = new Dictionary<string, byte[]>
        {
            ["Chime"] = Build(Tone(880, 0.22, false), Tone(660, 0.40, false), Silence(0.9)),
            ["Alarm"] = Build(Tone(1000, 0.12, true), Silence(0.07), Tone(1000, 0.12, true), Silence(0.07),
                              Tone(1000, 0.12, true), Silence(0.07), Tone(1000, 0.12, true), Silence(0.6)),
            ["Arcade"] = Build(Tone(523, 0.09, true), Tone(659, 0.09, true), Tone(784, 0.09, true),
                               Tone(1047, 0.22, true), Silence(0.8)),
        };

        SoundPlayer _player;
        string _loopingSound;

        /// <summary>Starts or stops the looping alert. Cheap to call on every tick.</summary>
        public void SetAlert(bool on, string sound)
        {
            if (!on)
            {
                if (_loopingSound != null) StopPlayer();
                return;
            }
            if (_loopingSound == sound) return;
            StopPlayer();
            _player = CreatePlayer(sound);
            try { _player.PlayLooping(); _loopingSound = sound; }
            catch (Exception ex) { Log.Error("Could not play alert", ex); }
        }

        public void PlayOnce(string sound)
        {
            StopPlayer();
            _player = CreatePlayer(sound);
            try { _player.Play(); }
            catch (Exception ex) { Log.Error("Could not play test sound", ex); }
        }

        SoundPlayer CreatePlayer(string sound)
        {
            if (!_wavs.TryGetValue(sound ?? "", out var wav)) wav = _wavs["Chime"];
            var p = new SoundPlayer(new MemoryStream(wav));
            p.Load();
            return p;
        }

        void StopPlayer()
        {
            _loopingSound = null;
            if (_player == null) return;
            try { _player.Stop(); } catch { }
            _player.Dispose();
            _player = null;
        }

        public void Dispose() => StopPlayer();

        static short[] Tone(double freq, double seconds, bool square)
        {
            int n = (int)(Rate * seconds);
            var data = new short[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double wave = Math.Sin(2 * Math.PI * freq * t);
                if (square) wave = Math.Sign(wave) * 0.6;
                // Short fade in, exponential fade out: no clicks between notes.
                double env = Math.Min(1, i / (Rate * 0.005)) * Math.Exp(-3.0 * t / seconds);
                data[i] = (short)(wave * env * 0.45 * short.MaxValue);
            }
            return data;
        }

        static short[] Silence(double seconds) => new short[(int)(Rate * seconds)];

        static byte[] Build(params short[][] parts)
        {
            int samples = 0;
            foreach (var p in parts) samples += p.Length;
            int dataBytes = samples * 2;
            using (var ms = new MemoryStream(44 + dataBytes))
            using (var w = new BinaryWriter(ms))
            {
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataBytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16);
                w.Write((short)1); w.Write((short)1);          // PCM, mono
                w.Write(Rate); w.Write(Rate * 2);               // sample rate, byte rate
                w.Write((short)2); w.Write((short)16);          // block align, bits
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataBytes);
                foreach (var p in parts) foreach (var s in p) w.Write(s);
                w.Flush();
                return ms.ToArray();
            }
        }
    }
}
