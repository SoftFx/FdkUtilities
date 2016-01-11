using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFeedExamples
{
    internal class MathList
    {
        List<double> listNumbers = new List<double>();
        public List<double> Numbers { get { return listNumbers; } }

        public MathList Clear()
        {
            listNumbers.Clear();
            return this;
        }

        public MathList Add(double number)
        {
            listNumbers.Add(number);
            return this;
        }

        public override string ToString()
        {
            return string.Format("mean={1:F1} SD={2:F1} ConfInt={3:F1}:{4:F1} length={0}"
                , listNumbers.Count, Mean(), Sd(), Mean()-2*Sd(), Mean() + 2*Sd());
        }

        private double Mean()
        {
            double result = default(double);
            double n = listNumbers.Count;
            foreach (double item in listNumbers)
                result += item / n;
            return result;
        }

        private double Sd()
        {
            double result = default(double);
            double mean = Mean();
            double n = listNumbers.Count;
            foreach (double item in listNumbers)
                result += (item - mean) * (item - mean) / n;
            return Math.Sqrt(result);
        }
    }
}
