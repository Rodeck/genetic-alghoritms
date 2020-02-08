using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace optimalization
{

    public class Bit22 
    {
         
        private string value {get; set;}

        public decimal decValue => -1 + (Convert.ToInt32(value, 2) * 0.000001m);

        public Bit22(decimal number)
        {
            var times = Math.Abs((-1 - number) / 0.000001m);
            string value = Convert.ToString((int)times, 2);
            string res = new StringBuilder("0000000000000000000000")
                .Remove(22 - value.Length, value.Length)
                .Insert(22 - value.Length, value)
                .ToString();
            this.value = res;
        }

        public Bit22(string val)
        {
            if(Convert.ToInt32(val, 2) > Convert.ToInt32("1111010000100100000000", 2))
                val = "1111010000100100000000";
            this.value = val;
        }

        public Bit22 Cross(Bit22 partner, int bitNumber) 
        {
            return new Bit22(partner.value.Substring(0, bitNumber) + this.value.Substring(bitNumber, 22 - bitNumber));
        }

        public Bit22 CopyAndMutate(int bitIdx) 
        {
            char valueAt = this.value[bitIdx];
            var stringValue = new StringBuilder(this.value)
                .Remove(bitIdx, 1)
                .Insert(bitIdx, valueAt == '1' ? '0' : '1')
                .ToString();
            return new Bit22(stringValue);
        }

        public static decimal GetClostest(decimal p1, int bitIdx) {
            if (bitIdx < 0 || bitIdx > 21)
                throw new Exception("Out of range");

            var p1b = new Bit22(p1).CopyAndMutate(bitIdx);
            int intValue = Math.Abs(Convert.ToInt32(p1b.value, 2));
            return -1 + (intValue * 0.000001m);
        }

        public static decimal[] GetClostestMultipleBits(decimal p1, int bitCount) {
            if (bitCount < 0 || bitCount > 21)
                throw new Exception("Out of range");

            var res = new decimal[bitCount];
            for (int i = 0; i < bitCount; i++) 
            {
                var resultBit = new Bit22(p1).CopyAndMutate(21 - i);
                int intValue = Math.Abs(Convert.ToInt32(resultBit.value, 2));
                if (intValue > 3000000)
                    intValue = 3000000;
                res[i] = -1 + (intValue * 0.000001m);
            }
            return res;
        }

        public static decimal GetClostestOne(decimal p1, int bitCount, Func<double, double> f) {
            if (bitCount < 0 || bitCount > 21)
                throw new Exception("Out of range");

            return (decimal)GetClostestMultipleBits(p1, bitCount)
                .Select(x => new 
                {
                    x = x, 
                    val = f((double)x) 
                })
                .OrderByDescending(x => x.val)
                .First().x;

        }
    }

    public static class Optimizer 
    {
        public static (double, double) Grad(double x, Func<double, double> f, double step, int maxIter = 1000) 
        {
            int iter = 0;
            double max = -1000000;
            while(iter < maxIter) 
            {
                double p1 = f(x);
                var clostest = (double)Bit22.GetClostestOne((decimal)x, 1, f);
                double p2 = f(clostest);
                if (p1 > p2)
                {
                    return (p1, x);
                }
                max = p2;
                iter++;
            }
            return (max, x + step);
        }

        public static (double, double) GradMultipleClostests(double x, Func<double, double> f, double step, int maxIter = 1000) 
        {
            int iter = 0;
            double max = -1000000;
            while(iter < maxIter) 
            {
                double p1 = f(x);
                double clostest = (double)Bit22.GetClostestOne((decimal)x, 1, f);
                double p2 = f(clostest);
                if (p1 > p2)
                {
                    return (p1, x);
                }
                max = p2;
                iter++;
            }
            return (max, x + step);
        }

        public static (double, double) Annealing(double x, 
            Func<double, double> f, 
            double step, 
            double alpha = 0.999,
            double temp = 400,
            double minTemp = 0.1)
        {
            Random rnd = new Random();
            double max = -1000000;
            while(temp > minTemp) 
            {
                double p1 = f(x);
                double p2 = f(x + step);
                if (p1 > p2)
                {
                    return (p1, x);
                }
                else 
                {
                    var prob = rnd.NextDouble();
                    if (prob < MathF.Exp((float)(p1-p2/temp)))
                    {
                        x += step;
                        max = p2;
                        if (x > 2)
                            return (max, x);
                    }
                }
                max = p2;
                temp *= alpha;
            }
            return (max, x + step);
        }

        public static (double, double) GeneticAlg(
            Func<double, double> f,
            int numberOfGuys,
            int numberOfIterations
        )
        {
            var generation = new Bit22[numberOfGuys];
            //Wygeneruj osobniki
            for(int i = 0; i < numberOfGuys; i++)
            {
                var x = Program.GetRandomNumber(-1, 2);
                generation[i] = new Bit22((decimal)x);
            }

            Bit22 bestValue = null;
            for (int i = 0; i < numberOfIterations; i++)
            {
                generation = Fit(generation, out bestValue, f);
                generation = CrossOver(generation);
            }

            return ((double)bestValue.decValue, ((double)bestValue.decValue));
        }

        public static Bit22[] Fit(Bit22[] generation, out Bit22 fittest, Func<double, double> f)
        {
            fittest = generation
                .Select(x => new 
                {
                    x = x, 
                    val = f((double)x.decValue) 
                })
                .OrderByDescending(x => x.val)
                .First().x;
            
            var values = generation.Select(x =>
                f((double)x.decValue));

            var sum = values.Sum();
            var nextPopulationNumbers = values.Select(x => 
            {
               return Math.Round(generation.Length * x/sum, MidpointRounding.AwayFromZero);
            }).ToList();
            
            var nextPopulation = new List<Bit22>();

            for (int i = 0; i < generation.Length; i++)
            {
                for (int j = 0; j < nextPopulationNumbers[i]; j++)
                {
                    nextPopulation.Add(new Bit22(generation[i].decValue));
                }
            }
            return nextPopulation.ToArray();
        }

        public static Bit22[] CrossOver(Bit22[] generation)
        {
            List<Bit22> newGen = new List<Bit22>();
            for (int i = 0; i < generation.Length; i++)
            {
                // get random partner
                int partner = RandomExcept(0, generation.Length, i);
                int crossBit = RandomExcept(0, 21, -1);
                Bit22 child = generation[i].Cross(generation[partner], crossBit);
                newGen.Add(child);
            }
            return newGen.ToArray();
        }

        public static int RandomExcept(int min, int max, int except)
        {
            Random rnd = new Random();
            int x = -1;
            while(x == -1)
            {
                int rand = rnd.Next(min, max);
                if (rand != except)
                    x = rand;
            }
            return x;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var b = new Bit22(3);
            int iter = 1000;
            List<double> values = new List<double>();
            List<(double, double)> resGrad = new List<(double ,double)>();
            List<(double, double)> resAnn = new List<(double ,double)>();
            Func<double, double> f = x => x * Math.Sin(10 * Math.PI * x) + 1;

            for (int i = 0; i < iter; i++) 
            {
                values.Add(GetRandomNumber(-1, 2));
            }

            var sw = new Stopwatch();
            sw.Start();
            foreach(var x in values) 
            {
                resGrad.Add(Optimizer.GradMultipleClostests(x, f, 0.001f, 100));
            }
            sw.Stop();
            var gradT = sw.ElapsedMilliseconds;

            sw = new Stopwatch();
            sw.Start();
            foreach(var x in values) 
            {
                resAnn.Add(Optimizer.Annealing(x, f, 0.0001, .8, 200));
            }
            sw.Stop();
            var annT = sw.ElapsedMilliseconds;

            var maxGrad = resGrad.OrderByDescending(x => x.Item1).First();
            var maxAnn = resAnn.OrderByDescending(x => x.Item1).First();
            var genetic = Optimizer.GeneticAlg(f, 4, 10);
            Console.WriteLine($"f({maxGrad.Item2}) = {maxGrad.Item1}, fvalue={f(maxGrad.Item2)} T={gradT}");
            Console.WriteLine($"f({maxAnn.Item2}) = {maxAnn.Item1}, fvalue={f(maxGrad.Item2)} T={annT}");
            Console.WriteLine($"f({genetic.Item2}) = {genetic.Item1}, fvalue={f(genetic.Item2)} T={annT}");
            

            //var bit = new Bit22(-0.999999m);
            //var clostest = Bit22.GetClostest(-0.999998m, 21);
            //Console.WriteLine(clostest);
        }

        public static double GetRandomNumber(double minimum, double maximum)
        { 
            Random random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
    }
}
