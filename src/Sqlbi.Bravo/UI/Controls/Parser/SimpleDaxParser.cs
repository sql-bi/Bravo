using System.Collections.Generic;

namespace Sqlbi.Bravo.UI.Controls.Parser
{
    public static class SimpleDaxParser
    {
        private static readonly List<string> DaxKeyWords = new List<string> {
            "DEFINE", "EVALUATE", "MEASURE", "COLUMN", "TABLE", "MPARAMETER",
            "ORDER", "BY", "START", "AT", "VAR", "RETURN"
            };

        private static readonly List<string> DaxFunctions = new List<string> {
            "ABS", "ACCRINT", "ACCRINTM", "ACOS", "ACOSH", "ACOT", "ACOTH", "ADDCOLUMNS",
            "ADDMISSINGITEMS", "ALL", "ALLCROSSFILTERED", "ALLEXCEPT", "ALLNOBLANKROW",
            "ALLSELECTED", "AMORDEGRC", "AMORLINC", "AND", "APPROXIMATEDISTINCTCOUNT",
            "ASIN", "ASINH", "ATAN", "ATANH", "AVERAGE", "AVERAGEA", "AVERAGEX", "BETA.DIST",
            "BETA.INV", "BLANK", "CALCULATE", "CALCULATETABLE", "CALENDAR", "CALENDARAUTO",
            "CEILING", "CHISQ.DIST", "CHISQ.DIST.RT", "CHISQ.INV", "CHISQ.INV.RT",
            "CLOSINGBALANCEMONTH", "CLOSINGBALANCEQUARTER", "CLOSINGBALANCEYEAR", "COALESCE",
            "COMBIN", "COMBINA", "COMBINEVALUES", "CONCATENATE", "CONCATENATEX",
            "CONFIDENCE.NORM", "CONFIDENCE.T", "CONTAINS", "CONTAINSROW", "CONTAINSSTRING",
            "CONTAINSSTRINGEXACT", "CONVERT", "COS", "COSH", "COT", "COTH", "COUNT", "COUNTA",
            "COUNTAX", "COUNTBLANK", "COUNTROWS", "COUNTX", "COUPDAYBS", "COUPDAYS",
            "COUPDAYSNC", "COUPNCD", "COUPNUM", "COUPPCD", "CROSSFILTER", "CROSSJOIN",
            "CUMIPMT", "CUMPRINC", "CURRENCY", "CURRENTGROUP", "CUSTOMDATA", "DATATABLE",
            "DATE", "DATEADD", "DATEDIFF", "DATESBETWEEN", "DATESINPERIOD", "DATESMTD",
            "DATESQTD", "DATESYTD", "DATEVALUE", "DAY", "DB", "DDB", "DEGREES", "DETAILROWS",
            "DISC", "DISTINCT", "DISTINCTCOUNT", "DISTINCTCOUNTNOBLANK", "DIVIDE", "DOLLARDE",
            "DOLLARFR", "DURATION", "EARLIER", "EARLIEST", "EDATE", "EFFECT", "ENDOFMONTH",
            "ENDOFQUARTER", "ENDOFYEAR", "EOMONTH", "ERROR", "EVEN", "EXACT", "EXCEPT", "EXP",
            "EXPON.DIST", "FACT", "FALSE", "FILTER", "FILTERS", "FIND", "FIRSTDATE",
            "FIRSTNONBLANK", "FIRSTNONBLANKVALUE", "FIXED", "FLOOR", "FORMAT", "FV", "GCD",
            "GENERATE", "GENERATEALL", "GENERATESERIES", "GEOMEAN", "GEOMEANX", "GROUPBY",
            "HASONEFILTER", "HASONEVALUE", "HOUR", "IF", "IF.EAGER", "IFERROR", "IGNORE",
            "INT", "INTERSECT", "INTRATE", "IPMT", "ISAFTER", "ISBLANK", "ISCROSSFILTERED",
            "ISEMPTY", "ISERROR", "ISEVEN", "ISFILTERED", "ISINSCOPE", "ISLOGICAL", "ISNONTEXT",
            "ISNUMBER", "ISO.CEILING", "ISODD", "ISONORAFTER", "ISPMT", "ISSELECTEDMEASURE",
            "ISSUBTOTAL", "ISTEXT", "KEEPFILTERS", "KEYWORDMATCH", "LASTDATE", "LASTNONBLANK",
            "LASTNONBLANKVALUE", "LCM", "LEFT", "LEN", "LN", "LOG", "LOG10", "LOOKUPVALUE",
            "LOWER", "MAX", "MAXA", "MAXX", "MDURATION", "MEDIAN", "MEDIANX", "MID", "MIN",
            "MINA", "MINUTE", "MINX", "MOD", "MONTH", "MROUND", "NATURALINNERJOIN",
            "NATURALLEFTOUTERJOIN", "NEXTDAY", "NEXTMONTH", "NEXTQUARTER", "NEXTYEAR",
            "NOMINAL", "NONVISUAL", "NORM.DIST", "NORM.INV", "NORM.S.DIST", "NORM.S.INV",
            "NOT", "NOW", "NPER", "ODD", "ODDFPRICE", "ODDFYIELD", "ODDLPRICE", "ODDLYIELD",
            "OPENINGBALANCEMONTH", "OPENINGBALANCEQUARTER", "OPENINGBALANCEYEAR", "OR",
            "PARALLELPERIOD", "PATH", "PATHCONTAINS", "PATHITEM", "PATHITEMREVERSE",
            "PATHLENGTH", "PDURATION", "PERCENTILE.EXC", "PERCENTILE.INC", "PERCENTILEX.EXC",
            "PERCENTILEX.INC", "PERMUT", "PI", "PMT", "POISSON.DIST", "POWER", "PPMT",
            "PREVIOUSDAY", "PREVIOUSMONTH", "PREVIOUSQUARTER", "PREVIOUSYEAR", "PRICE",
            "PRICEDISC", "PRICEMAT", "PRODUCT", "PRODUCTX", "PV", "QUARTER", "QUOTIENT",
            "RADIANS", "RAND", "RANDBETWEEN", "RANK.EQ", "RANKX", "RATE", "RECEIVED", "RELATED",
            "RELATEDTABLE", "REMOVEFILTERS", "REPLACE", "REPT", "RIGHT", "ROLLUP",
            "ROLLUPADDISSUBTOTAL", "ROLLUPGROUP", "ROLLUPISSUBTOTAL", "ROUND", "ROUNDDOWN",
            "ROUNDUP", "ROW", "RRI", "SAMEPERIODLASTYEAR", "SAMPLE", "SEARCH", "SECOND",
            "SELECTCOLUMNS", "SELECTEDMEASURE", "SELECTEDMEASUREFORMATSTRING",
            "SELECTEDMEASURENAME", "SELECTEDVALUE", "SIGN", "SIN", "SINH", "SLN", "SQRT",
            "SQRTPI", "STARTOFMONTH", "STARTOFQUARTER", "STARTOFYEAR", "STDEV.P", "STDEV.S",
            "STDEVX.P", "STDEVX.S", "SUBSTITUTE", "SUBSTITUTEWITHINDEX", "SUM", "SUMMARIZE",
            "SUMMARIZECOLUMNS", "SUMX", "SWITCH", "SYD", "T.DIST", "T.DIST.2T", "T.DIST.RT",
            "T.INV", "T.INV.2T", "TAN", "TANH", "TBILLEQ", "TBILLPRICE", "TBILLYIELD", "TIME",
            "TIMEVALUE", "TODAY", "TOPN", "TOTALMTD", "TOTALQTD", "TOTALYTD", "TREATAS", "TRIM",
            "TRUE", "TRUNC", "UNICHAR", "UNICODE", "UNION", "UPPER", "USERELATIONSHIP",
            "USERNAME", "USEROBJECTID", "USERPRINCIPALNAME", "UTCNOW", "UTCTODAY", "VALUE",
            "VALUES", "VAR.P", "VAR.S", "VARX.P", "VARX.S", "VDB", "WEEKDAY", "WEEKNUM",
            "XIRR", "XNPV", "YEAR", "YEARFRAC", "YIELD", "YIELDDISC", "YIELDMAT" };

        public static List<(string Text, ParsedTextType TextType)> ParseLine(string line)
        {
            var result = new List<(string, ParsedTextType)>();

            int lastReturn = 0;
            bool inWord = false;

            void CheckForWord(int i)
            {
                var word = line[lastReturn..i];

                if (string.IsNullOrEmpty(word))
                {
                    // Do nothing - probably a parenthesis immediatley after a keyword
                }
                else
                if (word == ")" || word == "(")
                {
                    // Do nothing - this is handled in the for loop
                    return;
                }
                else if (DaxFunctions.Contains(word))
                {
                    result.Add((word, ParsedTextType.Function));
                }
                else if (DaxKeyWords.Contains(word))
                {
                    result.Add((word, ParsedTextType.Keyword));
                }
                else
                {
                    result.Add((word, ParsedTextType.PlainText));
                }

                lastReturn = i;
            }

            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == '(' || line[i] == ')')
                {
                    CheckForWord(i);

                    result.Add((line[i].ToString(), ParsedTextType.Parenthesis));
                    lastReturn = i + 1;
                    inWord = false;
                }
                else if (char.IsLetterOrDigit(line[i]) || line[i] == '.')
                {
                    if (!inWord && lastReturn != i)
                    {
                        result.Add((line[lastReturn..i], ParsedTextType.PlainText));
                        lastReturn = i;
                    }

                    inWord = true;
                }
                else
                {
                    if (inWord)
                    {
                        CheckForWord(i);
                        inWord = false;
                    }
                }
            }

            // Handle from the last check until the end of the string
            CheckForWord(line.Length);

            return result;
        }
    }
}
