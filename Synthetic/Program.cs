using CSCore;
using System;

namespace Synthetic
{
    class Program
    {
        static void Main(string[] args)
        {

            var compiler = new Compiler();
            do
            {
                try
                {
                    string line = Console.ReadLine();
                    if (line == "")
                    {
                        break;
                    }

                    var expression = compiler.Compile(line);
                    var waveFunction = new SyntWaveGenerator(expression);
                    
                    var converter = new CSCore.Streams.SampleConverter.SampleToPcm16(waveFunction);
                    using (CSCore.Streams.BufferSource buffer = new CSCore.Streams.BufferSource(converter, 44800 * 5))
                    using (var soundOut = new CSCore.SoundOut.WasapiOut())
                    {
                        soundOut.Initialize(buffer);

                        soundOut.Play();

                        Console.ReadKey();

                        soundOut.Stop();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            while (true);
            
        }
    }

    sealed class SyntWaveGenerator : ISampleSource
    {
        private readonly Func<double, double> waveFunction;

        public bool CanSeek => false;

        public WaveFormat WaveFormat { get;  } = new WaveFormat(44100, 32, 1, AudioEncoding.IeeeFloat);

        public long Position { get => 0; set => throw new NotSupportedException(); }

        public long Length => 0;

        public double Phase { get; private set; }

        public void Dispose()
        {
        }        

        public SyntWaveGenerator(Func<double, double> wave)
        {
            this.waveFunction = wave;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (Phase > 1)
                Phase = 0;

            for (int i = offset; i < count; i++)
            {
                float sine = (float)this.waveFunction(Phase);
                buffer[i] = sine;

                Phase += (1.0 / WaveFormat.SampleRate);
            }

            return count;
        }
    }

    class Compiler
    {
        private readonly SynthLang parser;

        public Compiler()
        {
            parser = new SynthLang();
        }

        public Func<double, double> Compile(string input)
        {
            var expression = parser.Parse(input);
            var result = System.Linq.Expressions.Expression.Lambda<Func<double, double>>(expression, parser.Time);
            return result.Compile();
        }
    }
}
