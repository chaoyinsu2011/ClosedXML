// Keep this file CodeMaid organised and cleaned
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ClosedXML.Excel.CalcEngine
{
    internal class Tally : IEnumerable<Object>
    {
        private readonly List<object> _list = new List<object>();

        private double[]? _numericValues;

        public Tally()
        { }

        public Tally(IEnumerable<Expression>? p)
        {
            if (p != null)
            {
                foreach (var e in p)
                {
                    Add(e);
                }
            }
        }

        public void Add(Expression e)
        {
            // handle enumerables
            if (e is IEnumerable ienum)
            {
                foreach (var value in ienum)
                {
                    _list.Add(value);
                }
                _numericValues = null;
                return;
            }

            // handle expressions
            var val = e.Evaluate();
            if (val is string || !(val is IEnumerable valEnumerable))
                _list.Add(val);
            else
                foreach (var v in valEnumerable)
                    _list.Add(v);

            _numericValues = null;
        }

        public void AddValue(Object v)
        {
            _list.Add(v);
            _numericValues = null;
        }

        public double Average()
        {
            var nums = NumericValuesInternal();
            if (nums.Length == 0) throw new ApplicationException("No values");
            return nums.Average();
        }

        public double Count(bool numbersOnly)
        {
            if (numbersOnly)
                return NumericValuesInternal().Length;
            else
                return _list.Count(o => !CalcEngineHelpers.ValueIsBlank(o));
        }

        public IEnumerator<object> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public double Max()
        {
            var nums = NumericValuesInternal();
            return nums.Length == 0 ? 0 : nums.Max();
        }

        public double Min()
        {
            var nums = NumericValuesInternal();
            return nums.Length == 0 ? 0 : nums.Min();
        }

        public double Product()
        {
            var nums = NumericValuesInternal();
            return nums.Length == 0
                ? 0
                : nums.Aggregate(1d, (a, b) => a * b);
        }

        public double Std()
        {
            var values = NumericValuesInternal();
            var count = values.Length;
            double ret = 0;
            if (count != 0)
            {
                //Compute the Average
                double avg = values.Average();
                //Perform the Sum of (value-avg)_2_2
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together
                ret = Math.Sqrt((sum) / (count - 1));
            }
            else
            {
                throw new ApplicationException("No values");
            }
            return ret;
        }

        public double StdP()
        {
            var nums = NumericValuesInternal();
            var avg = nums.Average();
            var sum2 = nums.Sum(d => d * d);
            var count = nums.Length;
            return count <= 1 ? 0 : Math.Sqrt(sum2 / count - avg * avg);
        }

        public double Sum() => NumericValuesInternal().Sum();

        public double Var()
        {
            var nums = NumericValuesInternal();
            var avg = nums.Average();
            var sum2 = Sum2(nums);
            var count = nums.Length;
            return count <= 1 ? 0 : (sum2 / count - avg * avg) * count / (count - 1);
        }

        public double VarP()
        {
            var nums = NumericValuesInternal();
            var avg = nums.Average();
            var sum2 = Sum2(nums);
            var count = nums.Length;
            return count <= 1 ? 0 : sum2 / count - avg * avg;
        }

        private static double Sum2(IEnumerable<double> nums)
        {
            return nums.Sum(d => d * d);
        }

        private IEnumerable<double> NumericValuesEnumerable()
        {
            foreach (var value in _list)
            {
                if (value is string || !(value is IEnumerable vEnumerable))
                {
                    if (TryParseToDouble(value, aggressiveConversion: false, out double tmp))
                        yield return tmp;
                }
                else
                {
                    foreach (var v in vEnumerable)
                    {
                        if (TryParseToDouble(v, aggressiveConversion: false, out double tmp))
                            yield return tmp;
                    }
                }
            }
        }

        private double[] NumericValuesInternal()
                    => LazyInitializer.EnsureInitialized(ref _numericValues, () => NumericValuesEnumerable().ToArray())!;

        // If aggressiveConversion == true, then try to parse non-numeric types to double too
        private bool TryParseToDouble(object value, bool aggressiveConversion, out double d)
        {
            d = 0;
            if (value.IsNumber())
            {
                d = Convert.ToDouble(value);
                return true;
            }
            else if (value is Boolean b)
            {
                if (!aggressiveConversion) return false;

                d = (b ? 1 : 0);
                return true;
            }
            else if (value is DateTime dt)
            {
                d = dt.ToOADate();
                return true;
            }
            else if (value is TimeSpan ts)
            {
                d = ts.TotalDays;
                return true;
            }
            else if (value is string s)
            {
                if (!aggressiveConversion) return false;
                return double.TryParse(s, out d);
            }

            return false;
        }
    }
}
