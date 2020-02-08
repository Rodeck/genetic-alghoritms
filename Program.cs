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

        public Bit22 CopyAndMutate(decimal min, decimal max) 
        {
            Random rnd = new Random();
            var newBit = new Bit22(this.value);
            do
            {
                var bitIdx = rnd.Next(0, 21);
                char valueAt = this.value[bitIdx];
                var stringValue = new StringBuilder(this.value)
                    .Remove(bitIdx, 1)
                    .Insert(bitIdx, valueAt == '1' ? '0' : '1')
                    .ToString();
                newBit = new Bit22(stringValue);
            }
            while(newBit.decValue < min || newBit.decValue > max);

            return newBit;
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
                            double p1 = f(x);
            while(iter < maxIter) 
            {
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

        public static (double, double) SimpleGrad(double x, Func<double, double> f, double step, int maxIter = 1000) 
        {
            int iter = 0;
            double max = -1000000;
            while(iter < maxIter) 
            {
                double p1 = f(x);
                double p2 = f(x + step);
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
                generation = Mutate(generation).ToArray();
            }

            return ((double)bestValue.decValue, (f((double)bestValue.decValue)));
        }

        public static IEnumerable<Bit22> Mutate(Bit22[] generation)
        {
            foreach(var bit in generation)
            {
                yield return bit.CopyAndMutate(-1, 2);
            }
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
            int populationNumber = 4;
            var nextPopulationNumbers = values.OrderByDescending(x => x).Select(x => 
            {
                var current = (int)Math.Abs(Math.Round(generation.Length * x/sum));
                if (populationNumber >= current)
                {
                    populationNumber -= current;
                    return current;
                }
                else
                {
                    populationNumber = 0;
                    return populationNumber;
                }
            }).ToList();

            var all = nextPopulationNumbers.Sum(x => x);

            if ( all < 4)
            {
                int toAdd = 4 - all;
                int max = nextPopulationNumbers.Max();
                nextPopulationNumbers[0] = nextPopulationNumbers[0] + toAdd;
            }
            
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
            int iter = 10;
            List<double> values = new List<double>();
            List<(double, double)> resGrad = new List<(double ,double)>();
            List<(double, double)> simpleGradResult = new List<(double ,double)>();
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
                resGrad.Add(Optimizer.GradMultipleClostests(x, f, 0.001f, 1));
            }
            sw.Stop();
            var gradT = sw.ElapsedMilliseconds;

            sw.Start();
            foreach(var x in values.Take(1)) 
            {
                simpleGradResult.Add(Optimizer.SimpleGrad(x, f, 0.001f, 1));
            }
            sw.Stop();
            var simpleGradT = sw.ElapsedMilliseconds;

            sw = new Stopwatch();
            sw.Start();
            foreach(var x in values) 
            {
                resAnn.Add(Optimizer.Annealing(x, f, 0.0000001, .9999, 100));
            }
            sw.Stop();
            var annT = sw.ElapsedMilliseconds;

            var maxGrad = resGrad.OrderByDescending(x => x.Item1).First();
            var simpleGrad = simpleGradResult.OrderByDescending(x => x.Item1).First();
            var maxAnn = resAnn.OrderByDescending(x => x.Item1).First();

            sw = new Stopwatch();
            sw.Start();
            var genetic = Optimizer.GeneticAlg(f, 10, 1000);
            sw.Stop();
            var genT = sw.ElapsedMilliseconds;

            //Console.WriteLine($"f({maxGrad.Item2}) = {maxGrad.Item1} T={gradT}");
            // Console.WriteLine($"f({simpleGrad.Item2}) = {simpleGrad.Item1} T={simpleGradT}");
            // Console.WriteLine($"f({maxAnn.Item2}) = {maxAnn.Item1}");
            Console.WriteLine($"f({genetic.Item2}) = {genetic.Item1}, T={genT}");
            

            //var bit = new Bit22(-0.999999m);
            //var clostest = Bit22.GetClostest(-0.999998m, 21);
            //Console.WriteLine(clostest);
            //TestGrad(f);
        }

        public static double GetRandomNumber(double minimum, double maximum)
        { 
            Random random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }

        public static void TestGrad(Func<double, double> f)
        {
            Stopwatch sw = new Stopwatch();
            var result = new List<(float, int, long, (double, double))>();
            // float[] steps = new float[] { 0.0001f, 0.001f, 0.01f, 0.1f, 1.0f };
            float[] steps = new float[] { 0.01f };
            int[] randomPointsCount = new int[] { 1, 5, 10, 20, 100, 1000, 10000};
            foreach (var step in steps)
            {
                foreach (var randomPoints in randomPointsCount)
                {
                    sw.Start();
                    var bestResult = GetResult(randomPoints, f, step).OrderByDescending(x => x.Item2).First();
                    sw.Stop();
                    
                    result.Add((step, randomPoints, sw.ElapsedMilliseconds, bestResult));
                }
            }

            double searchedY = 2.85f;

            foreach(var output in result)
            {
                Console.WriteLine($"{output.Item1}; {output.Item2}; {output.Item3}; {output.Item4.Item2/searchedY * 100}");
            }
        }

        public static IEnumerable<(double, double)> GetResult(int randomPoints, Func<double, double> f, float step)
        {
            for (int i = 0; i < randomPoints; i ++) 
            {
                double p1 = GetRandomNumber(-1, 2);
                yield return Optimizer.SimpleGrad(p1, f, step, 2000);
            }
        }


    }
}
