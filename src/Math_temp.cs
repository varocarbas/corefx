using System;

namespace System_temp
{
    public static class Math_temp
    {
        //-------- Code already present in the last public version of System.Math

        private static double doubleRoundLimit = 1e16d;
        private const int maxRoundingDigits = 15;

        // This table is required for the Round function which can specify the number of digits to round to
        private static double[] roundPower10Double = new double[] { 
            1E0, 1E1, 1E2, 1E3, 1E4, 1E5, 1E6, 1E7, 1E8,
            1E9, 1E10, 1E11, 1E12, 1E13, 1E14, 1E15
        };   

        //------------------------------

        public const decimal PI_dec = 3.1415926535897932384626433832m; //Truncated at the 28th decimal position.
        public const decimal E_dec = 2.7182818284590452353602874713m; //Truncated at the 28th decimal position.

        //-------- Required by the different RoundExact overloads to deal with the given types
        private static int[] roundPower10Int = new int[] 
	    { 
		      1, 10, 100, 1000, 10000, 100000, 
		      1000000, 10000000, 100000000, 1000000000
	    };
        
        private static long[] roundPower10Long = new long[] 
	    { 
		    1, 10, 100, 1000, 10000, 100000, 1000000, 10000000,	100000000, 1000000000, 
		    10000000000, 100000000000, 1000000000000, 10000000000000, 100000000000000, 
            1000000000000000, 10000000000000000, 100000000000000000, 1000000000000000000
	    };

        private static decimal[] roundPower10Decimal = new decimal[] 
	    { 
            1E0m, 1E1m, 1E2m, 1E3m, 1E4m, 1E5m, 1E6m, 1E7m, 
            1E8m, 1E9m, 1E10m, 1E11m, 1E12m, 1E13m, 1E14m, 
            1E15m, 1E16m, 1E17m, 1E18m, 1E19m, 1E20m, 1E21m,
            1E22m, 1E23m, 1E24m, 1E25m, 1E26m, 1E27m, 1E28m
	    };
        //------------------------------

		//Used in all the RoundExact overloads to determine the type of rounding.
		//The "Midpoint" items apply the given rule when the last digit lies in the middle (equivalently to Math.Round).
        //The "Always" items apply the given rule in any case. 
        public enum RoundType 
        { 
            MidpointToEven, 
            MidpointAwayFromZero, 
            MidpointToZero, 
            AlwaysToEven, 
            AlwaysAwayFromZero, 
            AlwaysToZero 
        };
        
		//Used in the decimal overloads (types decimal & double) of RoundExact to determine the part to be rounded.
        public enum RoundSeparator 
        { 
            AfterDecimalSeparator, 
            BeforeDecimalSeparator 
        };

        public static decimal Truncate(decimal d, int decimals)
        {
            return RoundExact(d, decimals, RoundType.AlwaysToZero, RoundSeparator.AfterDecimalSeparator);
        }

        public static double Truncate(double d, int decimals)
        {
            return RoundExact(d, decimals, RoundType.AlwaysToZero, RoundSeparator.AfterDecimalSeparator);
        }

        public static decimal RoundExact(decimal d, RoundType type)
        {
            return RoundExact(d, 0, type, RoundSeparator.AfterDecimalSeparator);
        }

        public static decimal RoundExact(decimal d, int digits, RoundType type)
        {
            return RoundExact(d, digits, type, RoundSeparator.AfterDecimalSeparator);
        }

        public static decimal RoundExact(decimal d, int digits, RoundSeparator separator)
        {
            return RoundExact(d, digits, RoundType.MidpointToEven, separator);
        }

        public static decimal RoundExact(decimal d, int digits, RoundType type, RoundSeparator separator)
        {
            if ((digits < 0) || (digits > maxRoundingDigits)) return d;
            //throw new ArgumentOutOfRangeException("digits", Environment.GetResourceString("ArgumentOutOfRange_RoundingDigits"));
			//Contract.EndContractBlock();

            if (d == 0m) return 0m;

            decimal sign = (d > 0m ? 1m : -1m);
            d = Math.Abs(d); //All the methods called after this line expect positive numbers.

            //The digits variable being 0 is the same than BeforeDecimalSeparator + its value being the length of the integer part.
            return sign * (digits == 0 || separator == RoundSeparator.BeforeDecimalSeparator
                    ? RoundExactBefore(d, digits, type)
                    : RoundExactAfter(d, digits, type));
        }

        private static decimal RoundExactBefore(decimal d, int digits, RoundType type)
        {
            int length = GetIntegerLength(d);
            if (digits == 0) digits = length;

            return (length - digits < 0 ? d : RoundExactInternal(d, type, length, digits));
        }

        private static decimal RoundExactAfter(decimal d, int digits, RoundType type)
        {
            decimal d2 = GetDecimalPart(d, digits); 
            int length2 = GetIntegerLength(d2);

            if (length2 - digits >= 1)
            {
                d2 = RoundExactInternal(d2, type, length2, digits) / roundPower10Decimal[length2];
                d = Math.Floor(d) + d2; 
            }

            return d;
        }

        public static int RoundExact(int i, int digits)
        {
            return RoundExact(i, digits, RoundType.MidpointToEven);
        }

        public static int RoundExact(int i, int digits, RoundType type)
        {
            //For integer rounding, the digits variable cannot be zero.
            //The maxRoundingDigits value is 15, what is above the maximum number of digits of the int type.
            if ((digits <= 0) || (digits > 9)) return i;
            //throw new ArgumentOutOfRangeException("digits", Environment.GetResourceString("ArgumentOutOfRange_RoundingDigits"));
                //Contract.EndContractBlock();

            if (i == 0) return 0;
            bool isMin = false;
            //Accounting for the curious fact that the following equality holds: Math.Abs(int.MinValue + 1) == Math.Abs(int.MaxValue)
            if (i == int.MinValue && (isMin = true)) i = i + 1;

            int sign = (i > 0 ? 1 : -1);
            i = Math.Abs(i); //All the methods called after this line expect positive numbers.

            int length = GetIntegerLength(i);
            if (length - digits > 0) i = RoundExactInternal(i, type, length, digits);

            if (isMin && i == Math.Abs(int.MinValue + 1)) return int.MinValue;
            else return sign * i;
        }

        public static double RoundExact(double d, RoundType type)
        {
            return RoundExact(d, 0, type, RoundSeparator.AfterDecimalSeparator);
        }

        public static double RoundExact(double d, int digits, RoundType type)
        {
            return RoundExact(d, digits, type, RoundSeparator.AfterDecimalSeparator);
        }

        public static double RoundExact(double d, int digits, RoundSeparator separator)
        {
            return RoundExact(d, digits, RoundType.MidpointToEven, separator);
        }

        public static double RoundExact(double d, int digits, RoundType type, RoundSeparator separator)
        {
            if ((digits < 0) || (digits > maxRoundingDigits)) return d;
            //throw new ArgumentOutOfRangeException("digits", Environment.GetResourceString("ArgumentOutOfRange_RoundingDigits"));
            //Contract.EndContractBlock();

            if (d == 0.0 || d > doubleRoundLimit) return d; 

            double sign = (d > 0.0 ? 1.0 : -1.0);
            d = Math.Abs(d); //All the methods called after this line expect positive numbers.

            //The digits variable being 0 is the same than BeforeDecimalSeparator + its value being the length of the integer part.
            return sign * (digits == 0 || separator == RoundSeparator.BeforeDecimalSeparator 
                ? RoundExactBefore(d, digits, type) 
                : RoundExactAfter(d, digits, type));
        }
        
        public static double RoundExactBefore(double d, int digits, RoundType type)
        {
            int length = GetIntegerLength(d);
            if (digits == 0) digits = length;

            return (length - digits < 0 ? d : RoundExactInternal(d, type, length, digits));
        }

        public static double RoundExactAfter(double d, int digits, RoundType type)
        {
            double d2 = GetDecimalPart(d, digits);
            int length2 = GetIntegerLength(d2);

            if (length2 - digits >= 1)
            {
                d2 = RoundExactInternal(d2, type, length2, digits) / roundPower10Double[length2];
                d = Math.Floor(d) + d2;
            }

            return d;
        }

        public static long RoundExact(long l, int digits)
        {
            return RoundExact(l, digits, RoundType.MidpointToEven);
        }

        public static long RoundExact(long l, int digits, RoundType type)
        {
            //For integer rounding, the digits variable cannot be zero.
            if ((digits <= 0) || (digits > maxRoundingDigits)) return l;
            //throw new ArgumentOutOfRangeException("digits", Environment.GetResourceString("ArgumentOutOfRange_RoundingDigits"));
            //Contract.EndContractBlock();

            if (l == 0) return 0;
            bool isMin = false;
            //Accounting for the curious fact that the following equality holds: Math.Abs(long.MinValue + 1) == Math.Abs(long.MaxValue)
            if (l == long.MinValue && (isMin = true)) l = l + 1;

            int sign = (l > 0 ? 1 : -1);
            l = Math.Abs(l); //All the methods called after this line expect positive numbers.

            int length = GetIntegerLength(l);
            if (length - digits > 0) l = RoundExactInternal(l, type, length, digits);

            if (isMin && l == Math.Abs(long.MinValue + 1)) return long.MinValue;
            else return sign * l;
        }

        private static decimal GetDecimalPart(decimal d, int digits)
        {
            decimal d2 = d - Math.Floor(d);
            if (d2 == 0m) return 0m;

            if (digits >= roundPower10Decimal.Length - 2) 
            {
                d2 = d2 * roundPower10Decimal[roundPower10Decimal.Length - 1];
            }
            else
            {
                d2 = d2 * roundPower10Decimal[digits + 1];

                //Getting all the relevant digits after the target one.
                //For example: with 1.234512 and digits == 2, it delivers 2345 because 45 might also be relevant.
                while (d2 % 10m < 6m && d2 < roundPower10Decimal[roundPower10Decimal.Length - 2])
                {
                    d2 = d2 * 10m;
                }
            }

            return Math.Truncate(d2);
        }

        //The value variable has to be a positive number.
        private static int GetIntegerLength(decimal value)
        {
            for (int i = 0; i < roundPower10Decimal.Length - 1; i++)
            {
                if (value < roundPower10Decimal[i + 1]) return i + 1;
            }

            //Maximum number of digits for a variable of type decimal (29 digits).
            return roundPower10Decimal.Length;
        }

        private static decimal RoundExactInternal(decimal d, RoundType type, int length, int digits)
        {
            int remDigits = length - digits;
            decimal rounded = RoundExactTypes(d, type, remDigits, Math.Floor(d / roundPower10Decimal[remDigits]));

            if (length >= roundPower10Decimal.Length - 1)
            {
                //Accounting for overflow errors triggered by rounding-up too big numbers.
                decimal max = decimal.MaxValue / roundPower10Decimal[remDigits];
                if (rounded > max) return d;
            }

            return rounded * roundPower10Decimal[remDigits];
        }

        private static decimal RoundExactTypes(decimal d, RoundType type, int remDigits, decimal rounded)
        {
            if (type == RoundType.AlwaysAwayFromZero)
            {
                rounded = rounded + 1m;
            }
            else if (type != RoundType.AlwaysToZero)
            {
                bool lastDigitUneven = ((rounded % 10m) % 2m != 0m);

                if (type == RoundType.AlwaysToEven)
                {
                    if (lastDigitUneven) rounded = rounded + 1m;
                }
                else rounded = RoundExactMidPoint(d, type, remDigits, rounded, lastDigitUneven);
            }

            return rounded;
        }

        private static decimal RoundExactMidPoint(decimal d, RoundType type, int remDigits, decimal rounded, bool lastDigitUneven)
        {
            int comparison = MidPointComparison(d, remDigits, rounded);
            
            if (comparison == 1) rounded = rounded + 1m;
            else if (comparison == 0)
            {
                if (type == RoundType.MidpointAwayFromZero || (type == RoundType.MidpointToEven && lastDigitUneven))
                {
                    rounded = rounded + 1m;
                }
            }

            return rounded;
        }

        private static int MidPointComparison(decimal d, int remDigits, decimal rounded)
        {
            decimal d2 = d;
            decimal middle = 5m;
			
			if (remDigits == 0)
            {
                //The digit after the last rounded one lies in the decimal part.
                d2 = GetDecimalPart(d, 0);
                middle = middle * roundPower10Decimal[GetIntegerLength(d2) - 1];
            }
            else
            {
				d2 = Math.Floor(d2 - rounded * roundPower10Decimal[remDigits]);
                middle = middle * roundPower10Decimal[remDigits - 1];
            }

            return (d2 == middle ? 0 : (d2 < middle ? -1 : 1));
        }

        //The value variable has to be a positive number.
        private static int GetIntegerLength(int value)
        {
            for (int i = 0; i < roundPower10Int.Length - 1; i++)
            {
                if (value < roundPower10Int[i + 1]) return i + 1;
            }

            //Maximum number of digits for a variable of type int (10 digits).
            return roundPower10Int.Length;
        }

        private static int RoundExactInternal(int i, RoundType type, int length, int digits)
        {
            int remDigits = length - digits;
            int rounded = RoundExactTypes(i, type, remDigits, i / roundPower10Int[remDigits]);

            if (length >= roundPower10Int.Length - 1)
            {
                //Accounting for overflow errors triggered by rounding-up too big numbers.
                int max = int.MaxValue / roundPower10Int[remDigits];
                if (rounded > max) return i;
            }

            return rounded * roundPower10Int[remDigits];
        }

        private static int RoundExactTypes(int i, RoundType type, int remDigits, int rounded)
        {
            if (type == RoundType.AlwaysAwayFromZero)
            {
                rounded = rounded + 1;
            }
            else if (type != RoundType.AlwaysToZero)
            {
                bool lastDigitUneven = ((rounded % 10) % 2 != 0);

                if (type == RoundType.AlwaysToEven)
                {
                    if (lastDigitUneven) rounded = rounded + 1;
                }
                else rounded = RoundExactMidPoint(i, type, remDigits, rounded, lastDigitUneven);
            }

            return rounded;
        }

        private static int RoundExactMidPoint(int i, RoundType type, int remDigits, int rounded, bool lastDigitUneven)
        {
            int comparison = MidPointComparison(i, remDigits, rounded);

            if (comparison == 1) rounded = rounded + 1;
            else if (comparison == 0)
            {
                if (type == RoundType.MidpointAwayFromZero || (type == RoundType.MidpointToEven && lastDigitUneven))
                {
                    rounded = rounded + 1;
                }
            }

            return rounded;
        }

        private static int MidPointComparison(int i, int remDigits, int rounded)
        {
            int i2 = i - (rounded * roundPower10Int[remDigits]);
            int middle = 5 * roundPower10Int[remDigits - 1];

            return (i2 == middle ? 0 : (i2 < middle ? -1 : 1));
        }

        private static double GetDecimalPart(double d, int digits)
        {
            double d2 = d - Math.Floor(d);
            if (d2 == 0.0) return 0.0;

            if (digits >= roundPower10Double.Length - 2)
            {
                d2 = d2 * roundPower10Double[roundPower10Double.Length - 1];
            }
            else
            {
                d2 = d2 * roundPower10Double[digits + 1];

                //Getting all the relevant digits after the target one.
                //For example: with 1.234512 and digits == 2, it delivers 2345 because 45 might also be relevant.
                while (d2 % 10.0 < 6.0 && d2 < roundPower10Double[roundPower10Double.Length - 2])
                {
                    d2 = d2 * 10.0;
                }
            }

            return Math.Truncate(d2);
        }

        //The value variable has to be a positive number.
        private static int GetIntegerLength(double value)
        {
            for (int i = 0; i < roundPower10Double.Length - 1; i++)
            {
                if (value < roundPower10Double[i + 1]) return i + 1;
            }

            //Maximum precision supported by a variable of type double (16 digits).
            return roundPower10Double.Length;
        }

        private static double RoundExactInternal(double d, RoundType type, int length, int digits)
        {
            int remDigits = length - digits;
            double rounded = RoundExactTypes(d, type, remDigits, Math.Floor(d / roundPower10Double[remDigits]));

            if (length >= roundPower10Double.Length - 1)
            {
                //Accounting for overflow errors triggered by rounding-up too big numbers.
                double max = double.MaxValue / roundPower10Double[remDigits];
                if (rounded > max) return d;
            }

            return rounded * roundPower10Double[remDigits];
        }

        private static double RoundExactTypes(double d, RoundType type, int remDigits, double rounded)
        {
            if (type == RoundType.AlwaysAwayFromZero)
            {
                rounded = rounded + 1.0;
            }
            else if (type != RoundType.AlwaysToZero)
            {
                bool lastDigitUneven = ((rounded % 10.0) % 2.0 != 0.0);

                if (type == RoundType.AlwaysToEven)
                {
                    if (lastDigitUneven) rounded = rounded + 1.0;
                }
                else rounded = RoundExactMidPoint(d, type, remDigits, rounded, lastDigitUneven);
            }

            return rounded;
        }

        private static double RoundExactMidPoint(double d, RoundType type, int remDigits, double rounded, bool lastDigitUneven)
        {
            int comparison = MidPointComparison(d, remDigits, rounded);

            if (comparison == 1) rounded = rounded + 1.0;
            else if (comparison == 0)
            {
                if (type == RoundType.MidpointAwayFromZero || (type == RoundType.MidpointToEven && lastDigitUneven))
                {
                    rounded = rounded + 1.0;
                }
            }

            return rounded;
        }

        private static int MidPointComparison(double d, int remDigits, double rounded)
        {
            double d2 = d;
            double middle = 5.0;

            if (remDigits == 0)
            {
                //The digit after the last rounded one lies in the decimal part.
                d2 = GetDecimalPart(d, 0);
                middle = middle * roundPower10Double[GetIntegerLength(d2) - 1];
            }
            else
            {
                d2 = Math.Floor(d2 - rounded * roundPower10Double[remDigits]);
                middle = middle * roundPower10Double[remDigits - 1];
            }

            return (d2 == middle ? 0 : (d2 < middle ? -1 : 1));
        }

        //The value variable has to be a positive number.
        private static int GetIntegerLength(long value)
        {
            for (int i = 0; i < roundPower10Long.Length - 1; i++)
            {
                if (value < roundPower10Long[i + 1]) return i + 1;
            }

            //Maximum precision supported by a variable of type long (19 digits).
            return roundPower10Long.Length;
        }

        private static long RoundExactInternal(long l, RoundType type, int length, int digits)
        {
            int remDigits = length - digits;
            long rounded = RoundExactTypes(l, type, remDigits, l / roundPower10Long[remDigits]);

            if (length >= roundPower10Long.Length - 1)
            {
                //Accounting for overflow errors triggered by rounding-up too big numbers.
                long max = long.MaxValue / roundPower10Long[remDigits];
                if (rounded > max) return l;
            }

            return rounded * roundPower10Long[remDigits];
        }

        private static long RoundExactTypes(long l, RoundType type, int remDigits, long rounded)
        {
            if (type == RoundType.AlwaysAwayFromZero)
            {
                rounded = rounded + 1;
            }
            else if (type != RoundType.AlwaysToZero)
            {
                bool lastDigitUneven = ((rounded % 10) % 2 != 0);

                if (type == RoundType.AlwaysToEven)
                {
                    if (lastDigitUneven) rounded = rounded + 1;
                }
                else rounded = RoundExactMidPoint(l, type, remDigits, rounded, lastDigitUneven);
            }

            return rounded;
        }

        private static long RoundExactMidPoint(long l, RoundType type, int remDigits, long rounded, bool lastDigitUneven)
        {
            int comparison = MidPointComparison(l, remDigits, rounded);

            if (comparison == 1) rounded = rounded + 1;
            else if (comparison == 0)
            {
                if (type == RoundType.MidpointAwayFromZero || (type == RoundType.MidpointToEven && lastDigitUneven))
                {
                    rounded = rounded + 1;
                }
            }

            return rounded;
        }

        private static int MidPointComparison(long l, int remDigits, long rounded)
        {
            long l2 = l - (rounded * roundPower10Long[remDigits]);
            long middle = 5 * roundPower10Long[remDigits - 1];

            return (l2 == middle ? 0 : (l2 < middle ? -1 : 1));
        }
    }
}
