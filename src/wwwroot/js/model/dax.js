/*!
 * DAX functions taken from dax.guide
 * Last update: Nov 29, 2021
*/
const daxFns = [{"name":"ABS","syntax":"ABS ( &lt;Number&gt; )"},{"name":"ACCRINT","syntax":"ACCRINT ( &lt;Issue&gt;, &lt;First_interest&gt;, &lt;Settlement&gt;, &lt;Rate&gt;, &lt;Par&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] [, &lt;Calc_method&gt;] )"},{"name":"ACCRINTM","syntax":"ACCRINTM ( &lt;Issue&gt;, &lt;Maturity&gt;, &lt;Rate&gt;, &lt;Par&gt; [, &lt;Basis&gt;] )"},{"name":"ACOS","syntax":"ACOS ( &lt;Number&gt; )"},{"name":"ACOSH","syntax":"ACOSH ( &lt;Number&gt; )"},{"name":"ACOT","syntax":"ACOT ( &lt;Number&gt; )"},{"name":"ACOTH","syntax":"ACOTH ( &lt;Number&gt; )"},{"name":"ADDCOLUMNS","syntax":"ADDCOLUMNS ( &lt;Table&gt;, &lt;Name&gt;, &lt;Expression&gt; [, &lt;Name&gt;, &lt;Expression&gt; [, \u2026 ] ] )"},{"name":"ADDMISSINGITEMS","syntax":"ADDMISSINGITEMS (  [&lt;ShowAll_ColumnName&gt; [, &lt;ShowAll_ColumnName&gt; [, \u2026 ] ] ], &lt;Table&gt; [, &lt;GroupBy_ColumnName&gt; [, [&lt;FilterTable&gt;] [, &lt;GroupBy_ColumnName&gt; [, [&lt;FilterTable&gt;] [, \u2026 ] ] ] ] ] ] )"},{"name":"ALL","syntax":"ALL (  [&lt;TableNameOrColumnName&gt;] [, &lt;ColumnName&gt; [, &lt;ColumnName&gt; [, \u2026 ] ] ] )"},{"name":"ALLCROSSFILTERED","syntax":"ALLCROSSFILTERED ( &lt;TableName&gt; )"},{"name":"ALLEXCEPT","syntax":"ALLEXCEPT ( &lt;TableName&gt;, &lt;ColumnName&gt; [, &lt;ColumnName&gt; [, \u2026 ] ] )"},{"name":"ALLNOBLANKROW","syntax":"ALLNOBLANKROW ( &lt;TableNameOrColumnName&gt; [, &lt;ColumnName&gt; [, &lt;ColumnName&gt; [, \u2026 ] ] ] )"},{"name":"ALLSELECTED","syntax":"ALLSELECTED (  [&lt;TableNameOrColumnName&gt;] [, &lt;ColumnName&gt; [, &lt;ColumnName&gt; [, \u2026 ] ] ] )"},{"name":"AMORDEGRC","syntax":"AMORDEGRC ( &lt;Cost&gt;, &lt;Date_purchased&gt;, &lt;First_period&gt;, &lt;Salvage&gt;, &lt;Period&gt;, &lt;Rate&gt; [, &lt;Basis&gt;] )"},{"name":"AMORLINC","syntax":"AMORLINC ( &lt;Cost&gt;, &lt;Date_purchased&gt;, &lt;First_period&gt;, &lt;Salvage&gt;, &lt;Period&gt;, &lt;Rate&gt; [, &lt;Basis&gt;] )"},{"name":"AND","syntax":"AND ( &lt;Logical1&gt;, &lt;Logical2&gt; )"},{"name":"APPROXIMATEDISTINCTCOUNT","syntax":"APPROXIMATEDISTINCTCOUNT ( &lt;ColumnName&gt; )"},{"name":"ASIN","syntax":"ASIN ( &lt;Number&gt; )"},{"name":"ASINH","syntax":"ASINH ( &lt;Number&gt; )"},{"name":"ATAN","syntax":"ATAN ( &lt;Number&gt; )"},{"name":"ATANH","syntax":"ATANH ( &lt;Number&gt; )"},{"name":"AVERAGE","syntax":"AVERAGE ( &lt;ColumnName&gt; )"},{"name":"AVERAGEA","syntax":"AVERAGEA ( &lt;ColumnName&gt; )"},{"name":"AVERAGEX","syntax":"AVERAGEX ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"BETA.DIST","syntax":"BETA.DIST ( &lt;X&gt;, &lt;Alpha&gt;, &lt;Beta&gt;, &lt;Cumulative&gt; [, &lt;A&gt;] [, &lt;B&gt;] )"},{"name":"BETA.INV","syntax":"BETA.INV ( &lt;Probability&gt;, &lt;Alpha&gt;, &lt;Beta&gt; [, &lt;A&gt;] [, &lt;B&gt;] )"},{"name":"BLANK","syntax":"BLANK (  )"},{"name":"CALCULATE","syntax":"CALCULATE ( &lt;Expression&gt; [, &lt;Filter&gt; [, &lt;Filter&gt; [, \u2026 ] ] ] )"},{"name":"CALCULATETABLE","syntax":"CALCULATETABLE ( &lt;Table&gt; [, &lt;Filter&gt; [, &lt;Filter&gt; [, \u2026 ] ] ] )"},{"name":"CALENDAR","syntax":"CALENDAR ( &lt;StartDate&gt;, &lt;EndDate&gt; )"},{"name":"CALENDARAUTO","syntax":"CALENDARAUTO (  [&lt;FiscalYearEndMonth&gt;] )"},{"name":"CEILING","syntax":"CEILING ( &lt;Number&gt;, &lt;Significance&gt; )"},{"name":"CHISQ.DIST","syntax":"CHISQ.DIST ( &lt;X&gt;, &lt;Deg_freedom&gt;, &lt;Cumulative&gt; )"},{"name":"CHISQ.DIST.RT","syntax":"CHISQ.DIST.RT ( &lt;X&gt;, &lt;Deg_freedom&gt; )"},{"name":"CHISQ.INV","syntax":"CHISQ.INV ( &lt;Probability&gt;, &lt;Deg_freedom&gt; )"},{"name":"CHISQ.INV.RT","syntax":"CHISQ.INV.RT ( &lt;Probability&gt;, &lt;Deg_freedom&gt; )"},{"name":"CLOSINGBALANCEMONTH","syntax":"CLOSINGBALANCEMONTH ( &lt;Expression&gt;, &lt;Dates&gt; [, &lt;Filter&gt;] )"},{"name":"CLOSINGBALANCEQUARTER","syntax":"CLOSINGBALANCEQUARTER ( &lt;Expression&gt;, &lt;Dates&gt; [, &lt;Filter&gt;] )"},{"name":"CLOSINGBALANCEYEAR","syntax":"CLOSINGBALANCEYEAR ( &lt;Expression&gt;, &lt;Dates&gt; [, &lt;Filter&gt;] [, &lt;YearEndDate&gt;] )"},{"name":"COALESCE","syntax":"COALESCE ( &lt;Value1&gt;, &lt;Value2&gt; [, &lt;Value2&gt; [, \u2026 ] ] )"},{"name":"COMBIN","syntax":"COMBIN ( &lt;Number&gt;, &lt;Number_chosen&gt; )"},{"name":"COMBINA","syntax":"COMBINA ( &lt;Number&gt;, &lt;Number_chosen&gt; )"},{"name":"COMBINEVALUES","syntax":"COMBINEVALUES ( &lt;Delimiter&gt;, &lt;Expression1&gt;, &lt;Expression2&gt; [, &lt;Expression2&gt; [, \u2026 ] ] )"},{"name":"CONCATENATE","syntax":"CONCATENATE ( &lt;Text1&gt;, &lt;Text2&gt; )"},{"name":"CONCATENATEX","syntax":"CONCATENATEX ( &lt;Table&gt;, &lt;Expression&gt; [, &lt;Delimiter&gt;] [, &lt;OrderBy_Expression&gt; [, [&lt;Order&gt;] [, &lt;OrderBy_Expression&gt; [, [&lt;Order&gt;] [, \u2026 ] ] ] ] ] )"},{"name":"CONFIDENCE.NORM","syntax":"CONFIDENCE.NORM ( &lt;Alpha&gt;, &lt;Standard_dev&gt;, &lt;Size&gt; )"},{"name":"CONFIDENCE.T","syntax":"CONFIDENCE.T ( &lt;Alpha&gt;, &lt;Standard_dev&gt;, &lt;Size&gt; )"},{"name":"CONTAINS","syntax":"CONTAINS ( &lt;Table&gt;, &lt;ColumnName&gt;, &lt;Value&gt; [, &lt;ColumnName&gt;, &lt;Value&gt; [, \u2026 ] ] )"},{"name":"CONTAINSROW","syntax":"CONTAINSROW ( &lt;Table&gt;, &lt;Value&gt; [, &lt;Value&gt; [, \u2026 ] ] )"},{"name":"CONTAINSSTRING","syntax":"CONTAINSSTRING ( &lt;WithinText&gt;, &lt;FindText&gt; )"},{"name":"CONTAINSSTRINGEXACT","syntax":"CONTAINSSTRINGEXACT ( &lt;WithinText&gt;, &lt;FindText&gt; )"},{"name":"CONVERT","syntax":"CONVERT ( &lt;Expression&gt;, &lt;DataType&gt; )"},{"name":"COS","syntax":"COS ( &lt;Number&gt; )"},{"name":"COSH","syntax":"COSH ( &lt;Number&gt; )"},{"name":"COT","syntax":"COT ( &lt;Number&gt; )"},{"name":"COTH","syntax":"COTH ( &lt;Number&gt; )"},{"name":"COUNT","syntax":"COUNT ( &lt;ColumnName&gt; )"},{"name":"COUNTA","syntax":"COUNTA ( &lt;ColumnName&gt; )"},{"name":"COUNTAX","syntax":"COUNTAX ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"COUNTBLANK","syntax":"COUNTBLANK ( &lt;ColumnName&gt; )"},{"name":"COUNTROWS","syntax":"COUNTROWS ( &lt;Table&gt; )"},{"name":"COUNTX","syntax":"COUNTX ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"COUPDAYBS","syntax":"COUPDAYBS ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"COUPDAYS","syntax":"COUPDAYS ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"COUPDAYSNC","syntax":"COUPDAYSNC ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"COUPNCD","syntax":"COUPNCD ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"COUPNUM","syntax":"COUPNUM ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"COUPPCD","syntax":"COUPPCD ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"CROSSFILTER","syntax":"CROSSFILTER ( &lt;LeftColumnName&gt;, &lt;RightColumnName&gt;, &lt;CrossFilterType&gt; )"},{"name":"CROSSJOIN","syntax":"CROSSJOIN ( &lt;Table&gt; [, &lt;Table&gt; [, \u2026 ] ] )"},{"name":"CUMIPMT","syntax":"CUMIPMT ( &lt;Rate&gt;, &lt;Nper&gt;, &lt;Pv&gt;, &lt;Start_period&gt;, &lt;End_period&gt;, &lt;Type&gt; )"},{"name":"CUMPRINC","syntax":"CUMPRINC ( &lt;Rate&gt;, &lt;Nper&gt;, &lt;Pv&gt;, &lt;Start_period&gt;, &lt;End_period&gt;, &lt;Type&gt; )"},{"name":"CURRENCY","syntax":"CURRENCY ( &lt;Value&gt; )"},{"name":"CURRENTGROUP","syntax":"CURRENTGROUP (  )"},{"name":"CUSTOMDATA","syntax":"CUSTOMDATA (  )"},{"name":"DATATABLE","syntax":"DATATABLE ( &lt;name&gt;, &lt;type&gt; [, &lt;name&gt;, &lt;type&gt; [, \u2026 ] ], &lt;data&gt; )"},{"name":"DATE","syntax":"DATE ( &lt;Year&gt;, &lt;Month&gt;, &lt;Day&gt; )"},{"name":"DATEADD","syntax":"DATEADD ( &lt;Dates&gt;, &lt;NumberOfIntervals&gt;, &lt;Interval&gt; )"},{"name":"DATEDIFF","syntax":"DATEDIFF ( &lt;Date1&gt;, &lt;Date2&gt;, &lt;Interval&gt; )"},{"name":"DATESBETWEEN","syntax":"DATESBETWEEN ( &lt;Dates&gt;, &lt;StartDate&gt;, &lt;EndDate&gt; )"},{"name":"DATESINPERIOD","syntax":"DATESINPERIOD ( &lt;Dates&gt;, &lt;StartDate&gt;, &lt;NumberOfIntervals&gt;, &lt;Interval&gt; )"},{"name":"DATESMTD","syntax":"DATESMTD ( &lt;Dates&gt; )"},{"name":"DATESQTD","syntax":"DATESQTD ( &lt;Dates&gt; )"},{"name":"DATESYTD","syntax":"DATESYTD ( &lt;Dates&gt; [, &lt;YearEndDate&gt;] )"},{"name":"DATEVALUE","syntax":"DATEVALUE ( &lt;DateText&gt; )"},{"name":"DAY","syntax":"DAY ( &lt;Date&gt; )"},{"name":"DB","syntax":"DB ( &lt;Cost&gt;, &lt;Salvage&gt;, &lt;Life&gt;, &lt;Period&gt; [, &lt;Month&gt;] )"},{"name":"DDB","syntax":"DDB ( &lt;Cost&gt;, &lt;Salvage&gt;, &lt;Life&gt;, &lt;Period&gt; [, &lt;Factor&gt;] )"},{"name":"DEGREES","syntax":"DEGREES ( &lt;Number&gt; )"},{"name":"DETAILROWS","syntax":"DETAILROWS ( &lt;Measure&gt; )"},{"name":"DISC","syntax":"DISC ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Pr&gt;, &lt;Redemption&gt; [, &lt;Basis&gt;] )"},{"name":"DISTINCT","syntax":"DISTINCT ( &lt;ColumnNameOrTableExpr&gt; )"},{"name":"DISTINCTCOUNT","syntax":"DISTINCTCOUNT ( &lt;ColumnName&gt; )"},{"name":"DISTINCTCOUNTNOBLANK","syntax":"DISTINCTCOUNTNOBLANK ( &lt;ColumnName&gt; )"},{"name":"DIVIDE","syntax":"DIVIDE ( &lt;Numerator&gt;, &lt;Denominator&gt; [, &lt;AlternateResult&gt;] )"},{"name":"DOLLARDE","syntax":"DOLLARDE ( &lt;Fractional_dollar&gt;, &lt;Fraction&gt; )"},{"name":"DOLLARFR","syntax":"DOLLARFR ( &lt;Decimal_dollar&gt;, &lt;Fraction&gt; )"},{"name":"DURATION","syntax":"DURATION ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Coupon&gt;, &lt;Yld&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"EARLIER","syntax":"EARLIER ( &lt;ColumnName&gt; [, &lt;Number&gt;] )"},{"name":"EARLIEST","syntax":"EARLIEST ( &lt;ColumnName&gt; )"},{"name":"EDATE","syntax":"EDATE ( &lt;StartDate&gt;, &lt;Months&gt; )"},{"name":"EFFECT","syntax":"EFFECT ( &lt;Nominal_rate&gt;, &lt;Npery&gt; )"},{"name":"ENDOFMONTH","syntax":"ENDOFMONTH ( &lt;Dates&gt; )"},{"name":"ENDOFQUARTER","syntax":"ENDOFQUARTER ( &lt;Dates&gt; )"},{"name":"ENDOFYEAR","syntax":"ENDOFYEAR ( &lt;Dates&gt; [, &lt;YearEndDate&gt;] )"},{"name":"EOMONTH","syntax":"EOMONTH ( &lt;StartDate&gt;, &lt;Months&gt; )"},{"name":"ERROR","syntax":"ERROR ( &lt;ErrorText&gt; )"},{"name":"EVEN","syntax":"EVEN ( &lt;Number&gt; )"},{"name":"EXACT","syntax":"EXACT ( &lt;Text1&gt;, &lt;Text2&gt; )"},{"name":"EXCEPT","syntax":"EXCEPT ( &lt;LeftTable&gt;, &lt;RightTable&gt; )"},{"name":"EXP","syntax":"EXP ( &lt;Number&gt; )"},{"name":"EXPON.DIST","syntax":"EXPON.DIST ( &lt;X&gt;, &lt;Lambda&gt;, &lt;Cumulative&gt; )"},{"name":"FACT","syntax":"FACT ( &lt;Number&gt; )"},{"name":"FALSE","syntax":"FALSE (  )"},{"name":"FILTER","syntax":"FILTER ( &lt;Table&gt;, &lt;FilterExpression&gt; )"},{"name":"FILTERS","syntax":"FILTERS ( &lt;ColumnName&gt; )"},{"name":"FIND","syntax":"FIND ( &lt;FindText&gt;, &lt;WithinText&gt; [, &lt;StartPosition&gt;] [, &lt;NotFoundValue&gt;] )"},{"name":"FIRSTDATE","syntax":"FIRSTDATE ( &lt;Dates&gt; )"},{"name":"FIRSTNONBLANK","syntax":"FIRSTNONBLANK ( &lt;ColumnName&gt;, &lt;Expression&gt; )"},{"name":"FIRSTNONBLANKVALUE","syntax":"FIRSTNONBLANKVALUE ( &lt;ColumnName&gt;, &lt;Expression&gt; )"},{"name":"FIXED","syntax":"FIXED ( &lt;Number&gt; [, &lt;Decimals&gt;] [, &lt;NoCommas&gt;] )"},{"name":"FLOOR","syntax":"FLOOR ( &lt;Number&gt;, &lt;Significance&gt; )"},{"name":"FORMAT","syntax":"FORMAT ( &lt;Value&gt;, &lt;Format&gt; )"},{"name":"FV","syntax":"FV ( &lt;Rate&gt;, &lt;Nper&gt;, &lt;Pmt&gt; [, &lt;Pv&gt;] [, &lt;Type&gt;] )"},{"name":"GCD","syntax":"GCD ( &lt;Number1&gt;, &lt;Number2&gt; )"},{"name":"GENERATE","syntax":"GENERATE ( &lt;Table1&gt;, &lt;Table2&gt; )"},{"name":"GENERATEALL","syntax":"GENERATEALL ( &lt;Table1&gt;, &lt;Table2&gt; )"},{"name":"GENERATESERIES","syntax":"GENERATESERIES ( &lt;StartValue&gt;, &lt;EndValue&gt; [, &lt;IncrementValue&gt;] )"},{"name":"GEOMEAN","syntax":"GEOMEAN ( &lt;ColumnName&gt; )"},{"name":"GEOMEANX","syntax":"GEOMEANX ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"GROUPBY","syntax":"GROUPBY ( &lt;Table&gt; [, &lt;GroupBy_ColumnName&gt; [, [&lt;Name&gt;] [, [&lt;Expression&gt;] [, &lt;GroupBy_ColumnName&gt; [, [&lt;Name&gt;] [, [&lt;Expression&gt;] [, \u2026 ] ] ] ] ] ] ] )"},{"name":"HASONEFILTER","syntax":"HASONEFILTER ( &lt;ColumnName&gt; )"},{"name":"HASONEVALUE","syntax":"HASONEVALUE ( &lt;ColumnName&gt; )"},{"name":"HOUR","syntax":"HOUR ( &lt;Datetime&gt; )"},{"name":"IF","syntax":"IF ( &lt;LogicalTest&gt;, &lt;ResultIfTrue&gt; [, &lt;ResultIfFalse&gt;] )"},{"name":"IF.EAGER","syntax":"IF.EAGER ( &lt;LogicalTest&gt;, &lt;ResultIfTrue&gt; [, &lt;ResultIfFalse&gt;] )"},{"name":"IFERROR","syntax":"IFERROR ( &lt;Value&gt;, &lt;ValueIfError&gt; )"},{"name":"IGNORE","syntax":"IGNORE ( &lt;Measure_Expression&gt; )"},{"name":"INT","syntax":"INT ( &lt;Number&gt; )"},{"name":"INTERSECT","syntax":"INTERSECT ( &lt;LeftTable&gt;, &lt;RightTable&gt; )"},{"name":"INTRATE","syntax":"INTRATE ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Investment&gt;, &lt;Redemption&gt; [, &lt;Basis&gt;] )"},{"name":"IPMT","syntax":"IPMT ( &lt;Rate&gt;, &lt;Per&gt;, &lt;Nper&gt;, &lt;Pv&gt; [, &lt;Fv&gt;] [, &lt;Type&gt;] )"},{"name":"ISAFTER","syntax":"ISAFTER ( &lt;Value1&gt;, &lt;Value2&gt; [, [&lt;Order&gt;] [, &lt;Value1&gt;, &lt;Value2&gt; [, [&lt;Order&gt;] [, \u2026 ] ] ] ] )"},{"name":"ISBLANK","syntax":"ISBLANK ( &lt;Value&gt; )"},{"name":"ISCROSSFILTERED","syntax":"ISCROSSFILTERED ( &lt;TableNameOrColumnName&gt; )"},{"name":"ISEMPTY","syntax":"ISEMPTY ( &lt;Table&gt; )"},{"name":"ISERROR","syntax":"ISERROR ( &lt;Value&gt; )"},{"name":"ISEVEN","syntax":"ISEVEN ( &lt;Number&gt; )"},{"name":"ISFILTERED","syntax":"ISFILTERED ( &lt;TableNameOrColumnName&gt; )"},{"name":"ISINSCOPE","syntax":"ISINSCOPE ( &lt;ColumnName&gt; )"},{"name":"ISLOGICAL","syntax":"ISLOGICAL ( &lt;Value&gt; )"},{"name":"ISNONTEXT","syntax":"ISNONTEXT ( &lt;Value&gt; )"},{"name":"ISNUMBER","syntax":"ISNUMBER ( &lt;Value&gt; )"},{"name":"ISO.CEILING","syntax":"ISO.CEILING ( &lt;Number&gt; [, &lt;Significance&gt;] )"},{"name":"ISODD","syntax":"ISODD ( &lt;Number&gt; )"},{"name":"ISONORAFTER","syntax":"ISONORAFTER ( &lt;Value1&gt;, &lt;Value2&gt; [, [&lt;Order&gt;] [, &lt;Value1&gt;, &lt;Value2&gt; [, [&lt;Order&gt;] [, \u2026 ] ] ] ] )"},{"name":"ISPMT","syntax":"ISPMT ( &lt;Rate&gt;, &lt;Per&gt;, &lt;Nper&gt;, &lt;Pv&gt; )"},{"name":"ISSELECTEDMEASURE","syntax":"ISSELECTEDMEASURE ( &lt;Measure&gt; [, &lt;Measure&gt; [, \u2026 ] ] )"},{"name":"ISSUBTOTAL","syntax":"ISSUBTOTAL ( &lt;ColumnName&gt; )"},{"name":"ISTEXT","syntax":"ISTEXT ( &lt;Value&gt; )"},{"name":"KEEPFILTERS","syntax":"KEEPFILTERS ( &lt;Expression&gt; )"},{"name":"KEYWORDMATCH","syntax":"KEYWORDMATCH ( &lt;MatchExpression&gt;, &lt;Text&gt; )"},{"name":"LASTDATE","syntax":"LASTDATE ( &lt;Dates&gt; )"},{"name":"LASTNONBLANK","syntax":"LASTNONBLANK ( &lt;ColumnName&gt;, &lt;Expression&gt; )"},{"name":"LASTNONBLANKVALUE","syntax":"LASTNONBLANKVALUE ( &lt;ColumnName&gt;, &lt;Expression&gt; )"},{"name":"LCM","syntax":"LCM ( &lt;Number1&gt;, &lt;Number2&gt; )"},{"name":"LEFT","syntax":"LEFT ( &lt;Text&gt; [, &lt;NumberOfCharacters&gt;] )"},{"name":"LEN","syntax":"LEN ( &lt;Text&gt; )"},{"name":"LN","syntax":"LN ( &lt;Number&gt; )"},{"name":"LOG","syntax":"LOG ( &lt;Number&gt; [, &lt;Base&gt;] )"},{"name":"LOG10","syntax":"LOG10 ( &lt;Number&gt; )"},{"name":"LOOKUPVALUE","syntax":"LOOKUPVALUE ( &lt;Result_ColumnName&gt;, &lt;Search_ColumnName&gt;, &lt;Search_Value&gt; [, &lt;Search_ColumnName&gt;, &lt;Search_Value&gt; [, \u2026 ] ] [, &lt;Alternate_Result&gt;] )"},{"name":"LOWER","syntax":"LOWER ( &lt;Text&gt; )"},{"name":"MAX","syntax":"MAX ( &lt;ColumnNameOrScalar1&gt; [, &lt;Scalar2&gt;] )"},{"name":"MAXA","syntax":"MAXA ( &lt;ColumnName&gt; )"},{"name":"MAXX","syntax":"MAXX ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"MDURATION","syntax":"MDURATION ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Coupon&gt;, &lt;Yld&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"MEDIAN","syntax":"MEDIAN ( &lt;Column&gt; )"},{"name":"MEDIANX","syntax":"MEDIANX ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"MID","syntax":"MID ( &lt;Text&gt;, &lt;StartPosition&gt;, &lt;NumberOfCharacters&gt; )"},{"name":"MIN","syntax":"MIN ( &lt;ColumnNameOrScalar1&gt; [, &lt;Scalar2&gt;] )"},{"name":"MINA","syntax":"MINA ( &lt;ColumnName&gt; )"},{"name":"MINUTE","syntax":"MINUTE ( &lt;Datetime&gt; )"},{"name":"MINX","syntax":"MINX ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"MOD","syntax":"MOD ( &lt;Number&gt;, &lt;Divisor&gt; )"},{"name":"MONTH","syntax":"MONTH ( &lt;Date&gt; )"},{"name":"MROUND","syntax":"MROUND ( &lt;Number&gt;, &lt;Multiple&gt; )"},{"name":"NATURALINNERJOIN","syntax":"NATURALINNERJOIN ( &lt;LeftTable&gt;, &lt;RightTable&gt; )"},{"name":"NATURALLEFTOUTERJOIN","syntax":"NATURALLEFTOUTERJOIN ( &lt;LeftTable&gt;, &lt;RightTable&gt; )"},{"name":"NEXTDAY","syntax":"NEXTDAY ( &lt;Dates&gt; )"},{"name":"NEXTMONTH","syntax":"NEXTMONTH ( &lt;Dates&gt; )"},{"name":"NEXTQUARTER","syntax":"NEXTQUARTER ( &lt;Dates&gt; )"},{"name":"NEXTYEAR","syntax":"NEXTYEAR ( &lt;Dates&gt; [, &lt;YearEndDate&gt;] )"},{"name":"NOMINAL","syntax":"NOMINAL ( &lt;Effect_rate&gt;, &lt;Npery&gt; )"},{"name":"NONVISUAL","syntax":"NONVISUAL ( &lt;Expression&gt; )"},{"name":"NORM.DIST","syntax":"NORM.DIST ( &lt;X&gt;, &lt;Mean&gt;, &lt;Standard_dev&gt;, &lt;Cumulative&gt; )"},{"name":"NORM.INV","syntax":"NORM.INV ( &lt;Probability&gt;, &lt;Mean&gt;, &lt;Standard_dev&gt; )"},{"name":"NORM.S.DIST","syntax":"NORM.S.DIST ( &lt;Z&gt;, &lt;Cumulative&gt; )"},{"name":"NORM.S.INV","syntax":"NORM.S.INV ( &lt;Probability&gt; )"},{"name":"NOT","syntax":"NOT ( &lt;Logical&gt; )"},{"name":"NOW","syntax":"NOW (  )"},{"name":"NPER","syntax":"NPER ( &lt;Rate&gt;, &lt;Pmt&gt;, &lt;Pv&gt; [, &lt;Fv&gt;] [, &lt;Type&gt;] )"},{"name":"ODD","syntax":"ODD ( &lt;Number&gt; )"},{"name":"ODDFPRICE","syntax":"ODDFPRICE ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Issue&gt;, &lt;First_coupon&gt;, &lt;Rate&gt;, &lt;Yld&gt;, &lt;Redemption&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"ODDFYIELD","syntax":"ODDFYIELD ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Issue&gt;, &lt;First_coupon&gt;, &lt;Rate&gt;, &lt;Pr&gt;, &lt;Redemption&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"ODDLPRICE","syntax":"ODDLPRICE ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Last_interest&gt;, &lt;Rate&gt;, &lt;Yld&gt;, &lt;Redemption&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"ODDLYIELD","syntax":"ODDLYIELD ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Last_interest&gt;, &lt;Rate&gt;, &lt;Pr&gt;, &lt;Redemption&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"OPENINGBALANCEMONTH","syntax":"OPENINGBALANCEMONTH ( &lt;Expression&gt;, &lt;Dates&gt; [, &lt;Filter&gt;] )"},{"name":"OPENINGBALANCEQUARTER","syntax":"OPENINGBALANCEQUARTER ( &lt;Expression&gt;, &lt;Dates&gt; [, &lt;Filter&gt;] )"},{"name":"OPENINGBALANCEYEAR","syntax":"OPENINGBALANCEYEAR ( &lt;Expression&gt;, &lt;Dates&gt; [, &lt;Filter&gt;] [, &lt;YearEndDate&gt;] )"},{"name":"OR","syntax":"OR ( &lt;Logical1&gt;, &lt;Logical2&gt; )"},{"name":"PARALLELPERIOD","syntax":"PARALLELPERIOD ( &lt;Dates&gt;, &lt;NumberOfIntervals&gt;, &lt;Interval&gt; )"},{"name":"PATH","syntax":"PATH ( &lt;ID_ColumnName&gt;, &lt;Parent_ColumnName&gt; )"},{"name":"PATHCONTAINS","syntax":"PATHCONTAINS ( &lt;Path&gt;, &lt;Item&gt; )"},{"name":"PATHITEM","syntax":"PATHITEM ( &lt;Path&gt;, &lt;Position&gt; [, &lt;Type&gt;] )"},{"name":"PATHITEMREVERSE","syntax":"PATHITEMREVERSE ( &lt;Path&gt;, &lt;Position&gt; [, &lt;Type&gt;] )"},{"name":"PATHLENGTH","syntax":"PATHLENGTH ( &lt;Path&gt; )"},{"name":"PDURATION","syntax":"PDURATION ( &lt;Rate&gt;, &lt;Pv&gt;, &lt;Fv&gt; )"},{"name":"PERCENTILE.EXC","syntax":"PERCENTILE.EXC ( &lt;Column&gt;, &lt;K&gt; )"},{"name":"PERCENTILE.INC","syntax":"PERCENTILE.INC ( &lt;Column&gt;, &lt;K&gt; )"},{"name":"PERCENTILEX.EXC","syntax":"PERCENTILEX.EXC ( &lt;Table&gt;, &lt;Expression&gt;, &lt;K&gt; )"},{"name":"PERCENTILEX.INC","syntax":"PERCENTILEX.INC ( &lt;Table&gt;, &lt;Expression&gt;, &lt;K&gt; )"},{"name":"PERMUT","syntax":"PERMUT ( &lt;Number&gt;, &lt;Number_chosen&gt; )"},{"name":"PI","syntax":"PI (  )"},{"name":"PMT","syntax":"PMT ( &lt;Rate&gt;, &lt;Nper&gt;, &lt;Pv&gt; [, &lt;Fv&gt;] [, &lt;Type&gt;] )"},{"name":"POISSON.DIST","syntax":"POISSON.DIST ( &lt;X&gt;, &lt;Mean&gt;, &lt;Cumulative&gt; )"},{"name":"POWER","syntax":"POWER ( &lt;Number&gt;, &lt;Power&gt; )"},{"name":"PPMT","syntax":"PPMT ( &lt;Rate&gt;, &lt;Per&gt;, &lt;Nper&gt;, &lt;Pv&gt; [, &lt;Fv&gt;] [, &lt;Type&gt;] )"},{"name":"PREVIOUSDAY","syntax":"PREVIOUSDAY ( &lt;Dates&gt; )"},{"name":"PREVIOUSMONTH","syntax":"PREVIOUSMONTH ( &lt;Dates&gt; )"},{"name":"PREVIOUSQUARTER","syntax":"PREVIOUSQUARTER ( &lt;Dates&gt; )"},{"name":"PREVIOUSYEAR","syntax":"PREVIOUSYEAR ( &lt;Dates&gt; [, &lt;YearEndDate&gt;] )"},{"name":"PRICE","syntax":"PRICE ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Rate&gt;, &lt;Yld&gt;, &lt;Redemption&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"PRICEDISC","syntax":"PRICEDISC ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Discount&gt;, &lt;Redemption&gt; [, &lt;Basis&gt;] )"},{"name":"PRICEMAT","syntax":"PRICEMAT ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Issue&gt;, &lt;Rate&gt;, &lt;Yld&gt; [, &lt;Basis&gt;] )"},{"name":"PRODUCT","syntax":"PRODUCT ( &lt;ColumnName&gt; )"},{"name":"PRODUCTX","syntax":"PRODUCTX ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"PV","syntax":"PV ( &lt;Rate&gt;, &lt;Nper&gt;, &lt;Pmt&gt; [, &lt;Fv&gt;] [, &lt;Type&gt;] )"},{"name":"QUARTER","syntax":"QUARTER ( &lt;Date&gt; )"},{"name":"QUOTIENT","syntax":"QUOTIENT ( &lt;Numerator&gt;, &lt;Denominator&gt; )"},{"name":"RADIANS","syntax":"RADIANS ( &lt;Number&gt; )"},{"name":"RAND","syntax":"RAND (  )"},{"name":"RANDBETWEEN","syntax":"RANDBETWEEN ( &lt;Bottom&gt;, &lt;Top&gt; )"},{"name":"RANK.EQ","syntax":"RANK.EQ ( &lt;Value&gt;, &lt;ColumnName&gt; [, &lt;Order&gt;] )"},{"name":"RANKX","syntax":"RANKX ( &lt;Table&gt;, &lt;Expression&gt; [, &lt;Value&gt;] [, &lt;Order&gt;] [, &lt;Ties&gt;] )"},{"name":"RATE","syntax":"RATE ( &lt;Nper&gt;, &lt;Pmt&gt;, &lt;Pv&gt; [, &lt;Fv&gt;] [, &lt;Type&gt;] [, &lt;Guess&gt;] )"},{"name":"RECEIVED","syntax":"RECEIVED ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Investment&gt;, &lt;Discount&gt; [, &lt;Basis&gt;] )"},{"name":"RELATED","syntax":"RELATED ( &lt;ColumnName&gt; )"},{"name":"RELATEDTABLE","syntax":"RELATEDTABLE ( &lt;Table&gt; )"},{"name":"REMOVEFILTERS","syntax":"REMOVEFILTERS (  [&lt;TableNameOrColumnName&gt;] [, &lt;ColumnName&gt; [, &lt;ColumnName&gt; [, \u2026 ] ] ] )"},{"name":"REPLACE","syntax":"REPLACE ( &lt;OldText&gt;, &lt;StartPosition&gt;, &lt;NumberOfCharacters&gt;, &lt;NewText&gt; )"},{"name":"REPT","syntax":"REPT ( &lt;Text&gt;, &lt;NumberOfTimes&gt; )"},{"name":"RIGHT","syntax":"RIGHT ( &lt;Text&gt; [, &lt;NumberOfCharacters&gt;] )"},{"name":"ROLLUP","syntax":"ROLLUP ( &lt;GroupBy_ColumnName&gt; [, &lt;GroupBy_ColumnName&gt; [, \u2026 ] ] )"},{"name":"ROLLUPADDISSUBTOTAL","syntax":"ROLLUPADDISSUBTOTAL (  [&lt;GrandtotalFilter&gt;], &lt;GroupBy_ColumnName&gt;, &lt;Name&gt; [, [&lt;GroupLevelFilter&gt;] [, &lt;GroupBy_ColumnName&gt;, &lt;Name&gt; [, [&lt;GroupLevelFilter&gt;] [, \u2026 ] ] ] ] )"},{"name":"ROLLUPGROUP","syntax":"ROLLUPGROUP ( &lt;GroupBy_ColumnName&gt; [, &lt;GroupBy_ColumnName&gt; [, \u2026 ] ] )"},{"name":"ROLLUPISSUBTOTAL","syntax":"ROLLUPISSUBTOTAL (  [&lt;GrandtotalFilter&gt;], &lt;GroupBy_ColumnName&gt;, &lt;IsSubtotal_ColumnName&gt; [, [&lt;GroupLevelFilter&gt;] [, &lt;GroupBy_ColumnName&gt;, &lt;IsSubtotal_ColumnName&gt; [, [&lt;GroupLevelFilter&gt;] [, \u2026 ] ] ] ] )"},{"name":"ROUND","syntax":"ROUND ( &lt;Number&gt;, &lt;NumberOfDigits&gt; )"},{"name":"ROUNDDOWN","syntax":"ROUNDDOWN ( &lt;Number&gt;, &lt;NumberOfDigits&gt; )"},{"name":"ROUNDUP","syntax":"ROUNDUP ( &lt;Number&gt;, &lt;NumberOfDigits&gt; )"},{"name":"ROW","syntax":"ROW ( &lt;Name&gt;, &lt;Expression&gt; [, &lt;Name&gt;, &lt;Expression&gt; [, \u2026 ] ] )"},{"name":"RRI","syntax":"RRI ( &lt;Nper&gt;, &lt;Pv&gt;, &lt;Fv&gt; )"},{"name":"SAMEPERIODLASTYEAR","syntax":"SAMEPERIODLASTYEAR ( &lt;Dates&gt; )"},{"name":"SAMPLE","syntax":"SAMPLE ( &lt;Size&gt;, &lt;Table&gt;, &lt;OrderBy&gt; [, [&lt;Order&gt;] [, &lt;OrderBy&gt; [, [&lt;Order&gt;] [, \u2026 ] ] ] ] )"},{"name":"SEARCH","syntax":"SEARCH ( &lt;FindText&gt;, &lt;WithinText&gt; [, &lt;StartPosition&gt;] [, &lt;NotFoundValue&gt;] )"},{"name":"SECOND","syntax":"SECOND ( &lt;Datetime&gt; )"},{"name":"SELECTCOLUMNS","syntax":"SELECTCOLUMNS ( &lt;Table&gt;, &lt;Name&gt;, &lt;Expression&gt; [, &lt;Name&gt;, &lt;Expression&gt; [, \u2026 ] ] )"},{"name":"SELECTEDMEASURE","syntax":"SELECTEDMEASURE (  )"},{"name":"SELECTEDMEASUREFORMATSTRING","syntax":"SELECTEDMEASUREFORMATSTRING (  )"},{"name":"SELECTEDMEASURENAME","syntax":"SELECTEDMEASURENAME (  )"},{"name":"SELECTEDVALUE","syntax":"SELECTEDVALUE ( &lt;ColumnName&gt; [, &lt;AlternateResult&gt;] )"},{"name":"SIGN","syntax":"SIGN ( &lt;Number&gt; )"},{"name":"SIN","syntax":"SIN ( &lt;Number&gt; )"},{"name":"SINH","syntax":"SINH ( &lt;Number&gt; )"},{"name":"SLN","syntax":"SLN ( &lt;Cost&gt;, &lt;Salvage&gt;, &lt;Life&gt; )"},{"name":"SQRT","syntax":"SQRT ( &lt;Number&gt; )"},{"name":"SQRTPI","syntax":"SQRTPI ( &lt;Number&gt; )"},{"name":"STARTOFMONTH","syntax":"STARTOFMONTH ( &lt;Dates&gt; )"},{"name":"STARTOFQUARTER","syntax":"STARTOFQUARTER ( &lt;Dates&gt; )"},{"name":"STARTOFYEAR","syntax":"STARTOFYEAR ( &lt;Dates&gt; [, &lt;YearEndDate&gt;] )"},{"name":"STDEV.P","syntax":"STDEV.P ( &lt;ColumnName&gt; )"},{"name":"STDEV.S","syntax":"STDEV.S ( &lt;ColumnName&gt; )"},{"name":"STDEVX.P","syntax":"STDEVX.P ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"STDEVX.S","syntax":"STDEVX.S ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"SUBSTITUTE","syntax":"SUBSTITUTE ( &lt;Text&gt;, &lt;OldText&gt;, &lt;NewText&gt; [, &lt;InstanceNumber&gt;] )"},{"name":"SUBSTITUTEWITHINDEX","syntax":"SUBSTITUTEWITHINDEX ( &lt;Table&gt;, &lt;Name&gt;, &lt;SemiJoinIndexTable&gt;, &lt;Expression&gt; [, [&lt;Order&gt;] [, &lt;Expression&gt; [, [&lt;Order&gt;] [, \u2026 ] ] ] ] )"},{"name":"SUM","syntax":"SUM ( &lt;ColumnName&gt; )"},{"name":"SUMMARIZE","syntax":"SUMMARIZE ( &lt;Table&gt; [, &lt;GroupBy_ColumnName&gt; [, [&lt;Name&gt;] [, [&lt;Expression&gt;] [, &lt;GroupBy_ColumnName&gt; [, [&lt;Name&gt;] [, [&lt;Expression&gt;] [, \u2026 ] ] ] ] ] ] ] )"},{"name":"SUMMARIZECOLUMNS","syntax":"SUMMARIZECOLUMNS (  [&lt;GroupBy_ColumnName&gt; [, [&lt;FilterTable&gt;] [, [&lt;Name&gt;] [, [&lt;Expression&gt;] [, &lt;GroupBy_ColumnName&gt; [, [&lt;FilterTable&gt;] [, [&lt;Name&gt;] [, [&lt;Expression&gt;] [, \u2026 ] ] ] ] ] ] ] ] ] )"},{"name":"SUMX","syntax":"SUMX ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"SWITCH","syntax":"SWITCH ( &lt;Expression&gt;, &lt;Value&gt;, &lt;Result&gt; [, &lt;Value&gt;, &lt;Result&gt; [, \u2026 ] ] [, &lt;Else&gt;] )"},{"name":"SYD","syntax":"SYD ( &lt;Cost&gt;, &lt;Salvage&gt;, &lt;Life&gt;, &lt;Per&gt; )"},{"name":"T.DIST","syntax":"T.DIST ( &lt;X&gt;, &lt;Deg_freedom&gt;, &lt;Cumulative&gt; )"},{"name":"T.DIST.2T","syntax":"T.DIST.2T ( &lt;X&gt;, &lt;Deg_freedom&gt; )"},{"name":"T.DIST.RT","syntax":"T.DIST.RT ( &lt;X&gt;, &lt;Deg_freedom&gt; )"},{"name":"T.INV","syntax":"T.INV ( &lt;Probability&gt;, &lt;Deg_freedom&gt; )"},{"name":"T.INV.2T","syntax":"T.INV.2T ( &lt;Probability&gt;, &lt;Deg_freedom&gt; )"},{"name":"TAN","syntax":"TAN ( &lt;Number&gt; )"},{"name":"TANH","syntax":"TANH ( &lt;Number&gt; )"},{"name":"TBILLEQ","syntax":"TBILLEQ ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Discount&gt; )"},{"name":"TBILLPRICE","syntax":"TBILLPRICE ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Discount&gt; )"},{"name":"TBILLYIELD","syntax":"TBILLYIELD ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Pr&gt; )"},{"name":"TIME","syntax":"TIME ( &lt;Hour&gt;, &lt;Minute&gt;, &lt;Second&gt; )"},{"name":"TIMEVALUE","syntax":"TIMEVALUE ( &lt;TimeText&gt; )"},{"name":"TODAY","syntax":"TODAY (  )"},{"name":"TOPN","syntax":"TOPN ( &lt;N_Value&gt;, &lt;Table&gt; [, &lt;OrderBy_Expression&gt; [, [&lt;Order&gt;] [, &lt;OrderBy_Expression&gt; [, [&lt;Order&gt;] [, \u2026 ] ] ] ] ] )"},{"name":"TOPNPERLEVEL","syntax":"TOPNPERLEVEL ( &lt;Rows&gt;, &lt;Table&gt;, &lt;LevelsDefinition&gt;, &lt;NodesExpanded&gt;, &lt;LevelsBoundaries&gt;, &lt;RestartIndicatorColumnName&gt; )"},{"name":"TOPNSKIP","syntax":"TOPNSKIP ( &lt;Rows&gt;, &lt;Skip&gt;, &lt;Table&gt; [, &lt;OrderBy_Expression&gt; [, [&lt;Order&gt;] [, &lt;OrderBy_Expression&gt; [, [&lt;Order&gt;] [, \u2026 ] ] ] ] ] )"},{"name":"TOTALMTD","syntax":"TOTALMTD ( &lt;Expression&gt;, &lt;Dates&gt; [, &lt;Filter&gt;] )"},{"name":"TOTALQTD","syntax":"TOTALQTD ( &lt;Expression&gt;, &lt;Dates&gt; [, &lt;Filter&gt;] )"},{"name":"TOTALYTD","syntax":"TOTALYTD ( &lt;Expression&gt;, &lt;Dates&gt; [, &lt;Filter&gt;] [, &lt;YearEndDate&gt;] )"},{"name":"TREATAS","syntax":"TREATAS ( &lt;Expression&gt;, &lt;ColumnName&gt; [, &lt;ColumnName&gt; [, \u2026 ] ] )"},{"name":"TRIM","syntax":"TRIM ( &lt;Text&gt; )"},{"name":"TRUE","syntax":"TRUE (  )"},{"name":"TRUNC","syntax":"TRUNC ( &lt;Number&gt; [, &lt;NumberOfDigits&gt;] )"},{"name":"UNICHAR","syntax":"UNICHAR ( &lt;Number&gt; )"},{"name":"UNICODE","syntax":"UNICODE ( &lt;Text&gt; )"},{"name":"UNION","syntax":"UNION ( &lt;Table&gt;, &lt;Table&gt; [, &lt;Table&gt; [, \u2026 ] ] )"},{"name":"UPPER","syntax":"UPPER ( &lt;Text&gt; )"},{"name":"USERCULTURE","syntax":"USERCULTURE ( )"},{"name":"USERELATIONSHIP","syntax":"USERELATIONSHIP ( &lt;ColumnName1&gt;, &lt;ColumnName2&gt; )"},{"name":"USERNAME","syntax":"USERNAME (  )"},{"name":"USEROBJECTID","syntax":"USEROBJECTID (  )"},{"name":"USERPRINCIPALNAME","syntax":"USERPRINCIPALNAME (  )"},{"name":"UTCNOW","syntax":"UTCNOW (  )"},{"name":"UTCTODAY","syntax":"UTCTODAY (  )"},{"name":"VALUE","syntax":"VALUE ( &lt;Text&gt; )"},{"name":"VALUES","syntax":"VALUES ( &lt;TableNameOrColumnName&gt; )"},{"name":"VAR.P","syntax":"VAR.P ( &lt;ColumnName&gt; )"},{"name":"VAR.S","syntax":"VAR.S ( &lt;ColumnName&gt; )"},{"name":"VARX.P","syntax":"VARX.P ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"VARX.S","syntax":"VARX.S ( &lt;Table&gt;, &lt;Expression&gt; )"},{"name":"VDB","syntax":"VDB ( &lt;Cost&gt;, &lt;Salvage&gt;, &lt;Life&gt;, &lt;Start_period&gt;, &lt;End_period&gt; [, &lt;Factor&gt;] [, &lt;No_switch&gt;] )"},{"name":"WEEKDAY","syntax":"WEEKDAY ( &lt;Date&gt; [, &lt;ReturnType&gt;] )"},{"name":"WEEKNUM","syntax":"WEEKNUM ( &lt;Date&gt; [, &lt;ReturnType&gt;] )"},{"name":"XIRR","syntax":"XIRR ( &lt;Table&gt;, &lt;Values&gt;, &lt;Dates&gt; [, &lt;Guess&gt;] )"},{"name":"XNPV","syntax":"XNPV ( &lt;Table&gt;, &lt;Values&gt;, &lt;Dates&gt;, &lt;Rate&gt; )"},{"name":"YEAR","syntax":"YEAR ( &lt;Date&gt; )"},{"name":"YEARFRAC","syntax":"YEARFRAC ( &lt;StartDate&gt;, &lt;EndDate&gt; [, &lt;Basis&gt;] )"},{"name":"YIELD","syntax":"YIELD ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Rate&gt;, &lt;Pr&gt;, &lt;Redemption&gt;, &lt;Frequency&gt; [, &lt;Basis&gt;] )"},{"name":"YIELDDISC","syntax":"YIELDDISC ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Pr&gt;, &lt;Redemption&gt; [, &lt;Basis&gt;] )"},{"name":"YIELDMAT","syntax":"YIELDMAT ( &lt;Settlement&gt;, &lt;Maturity&gt;, &lt;Issue&gt;, &lt;Rate&gt;, &lt;Pr&gt; [, &lt;Basis&gt;] )"},{"name":"AT","syntax":""},{"name":"ASC","syntax":""},{"name":"BOOLEAN","syntax":""},{"name":"BOTH","syntax":""},{"name":"BY","syntax":""},{"name":"COLUMN","syntax":""},{"name":"CREATE","syntax":""},{"name":"CURRENCY","syntax":""},{"name":"DATETIME","syntax":""},{"name":"DAY","syntax":""},{"name":"DEFINE","syntax":""},{"name":"DESC","syntax":""},{"name":"DOUBLE","syntax":""},{"name":"EVALUATE","syntax":""},{"name":"FALSE","syntax":""},{"name":"INTEGER","syntax":""},{"name":"MEASURE","syntax":""},{"name":"MONTH","syntax":""},{"name":"NONE","syntax":""},{"name":"ORDER","syntax":""},{"name":"RETURN","syntax":""},{"name":"SINGLE","syntax":""},{"name":"START","syntax":""},{"name":"STRING","syntax":""},{"name":"TABLE","syntax":""},{"name":"TRUE","syntax":""},{"name":"VAR","syntax":""},{"name":"YEAR","syntax":""}];