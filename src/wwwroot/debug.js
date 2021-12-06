/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
 * 
 * Just for debug, don't use it in production
*/
const debug = {
    host: {
        GetModelFromVpax: {
            model: {
                "tablesCount": 10,
                "columnsCount": 68,
                "unreferencedCount": 5,
                "maxRows": 4058451434,
                "size": 17894024346,
                "columns": [
                    {
                        "columnName": "RowNumber-2662979B-1795-4F74-8F37-6A1BA8059B61",
                        "tableName": "time",
                        "columnCardinality": 0,
                        "size": 120,
                        "weight": 6.706149364708146e-9
                    },
                    {
                        "columnName": "ID_Time",
                        "tableName": "time",
                        "columnCardinality": 1440,
                        "size": 52728,
                        "weight": 0.0000029466820308527592,
                        "isReferenced": false
                    },
                    {
                        "columnName": "HourMinute",
                        "tableName": "time",
                        "columnCardinality": 1440,
                        "size": 61960,
                        "weight": 0.000003462608455310973
                    },
                    {
                        "columnName": "Period05Minutes",
                        "tableName": "time",
                        "columnCardinality": 288,
                        "size": 31400,
                        "weight": 0.0000017547757504319648
                    },
                    {
                        "columnName": "Period10Minutes",
                        "tableName": "time",
                        "columnCardinality": 144,
                        "size": 24864,
                        "weight": 0.0000013895141483675279
                    },
                    {
                        "columnName": "Period15Minutes",
                        "tableName": "time",
                        "columnCardinality": 96,
                        "size": 22592,
                        "weight": 0.000001262544387062387
                    },
                    {
                        "columnName": "Period30Minutes",
                        "tableName": "time",
                        "columnCardinality": 48,
                        "size": 20352,
                        "weight": 0.0000011373629322545015,
                        "isReferenced": false
                    },
                    {
                        "columnName": "Period60Minutes",
                        "tableName": "time",
                        "columnCardinality": 24,
                        "size": 19104,
                        "weight": 0.0000010676189788615369,
                        "isReferenced": false
                    },
                    {
                        "columnName": "RowNumber-2662979B-1795-4F74-8F37-6A1BA8059B61",
                        "tableName": "Date",
                        "columnCardinality": 0,
                        "size": 120,
                        "weight": 6.706149364708146e-9
                    },
                    {
                        "columnName": "ID_Date",
                        "tableName": "Date",
                        "columnCardinality": 1796,
                        "size": 57568,
                        "weight": 0.000003217163388562655
                    },
                    {
                        "columnName": "Date",
                        "tableName": "Date",
                        "columnCardinality": 1796,
                        "size": 111296,
                        "weight": 0.0000062197299974546485
                    },
                    {
                        "columnName": "Year",
                        "tableName": "Date",
                        "columnCardinality": 5,
                        "size": 1452,
                        "weight": 8.114440731296857e-8
                    },
                    {
                        "columnName": "monthNumber",
                        "tableName": "Date",
                        "columnCardinality": 12,
                        "size": 2440,
                        "weight": 1.363583704157323e-7
                    },
                    {
                        "columnName": "monthName",
                        "tableName": "Date",
                        "columnCardinality": 12,
                        "size": 18348,
                        "weight": 0.0000010253702378638754
                    },
                    {
                        "columnName": "DayOfTheMonth",
                        "tableName": "Date",
                        "columnCardinality": 31,
                        "size": 2956,
                        "weight": 1.6519481268397733e-7
                    },
                    {
                        "columnName": "RowNumber-2662979B-1795-4F74-8F37-6A1BA8059B61",
                        "tableName": "Individuals",
                        "columnCardinality": 0,
                        "size": 120,
                        "weight": 6.706149364708146e-9
                    },
                    {
                        "columnName": "ID_Individual",
                        "tableName": "Individuals",
                        "columnCardinality": 152275,
                        "size": 6428036,
                        "weight": 0.00035922807948100913
                    },
                    {
                        "columnName": "cOD_Individual",
                        "tableName": "Individuals",
                        "columnCardinality": 25559,
                        "size": 711480,
                        "weight": 0.0000397607595833546
                    },
                    {
                        "columnName": "cOD_Family",
                        "tableName": "Individuals",
                        "columnCardinality": 9546,
                        "size": 444400,
                        "weight": 0.000024835106480635834
                    },
                    {
                        "columnName": "childrenLessThan3Years",
                        "tableName": "Individuals",
                        "columnCardinality": 2,
                        "size": 23918,
                        "weight": 0.000001336647337542412
                    },
                    {
                        "columnName": "childrenBetween3And14Years",
                        "tableName": "Individuals",
                        "columnCardinality": 2,
                        "size": 28896,
                        "weight": 0.0000016148407670217217
                    },
                    {
                        "columnName": "FamilyComponents",
                        "tableName": "Individuals",
                        "columnCardinality": 5,
                        "size": 37054,
                        "weight": 0.000002070747154665797
                    },
                    {
                        "columnName": "clusterTV",
                        "tableName": "Individuals",
                        "columnCardinality": 9,
                        "size": 34936,
                        "weight": 0.000001952383618378698
                    },
                    {
                        "columnName": "Inhabitants",
                        "tableName": "Individuals",
                        "columnCardinality": 4,
                        "size": 50722,
                        "weight": 0.000002834577567306055
                    },
                    {
                        "columnName": "AgeRange",
                        "tableName": "Individuals",
                        "columnCardinality": 9,
                        "size": 25522,
                        "weight": 0.0000014262862007173443
                    },
                    {
                        "columnName": "Eurisko",
                        "tableName": "Individuals",
                        "columnCardinality": 20,
                        "size": 84824,
                        "weight": 0.000004740353447600032
                    },
                    {
                        "columnName": "HasMoreThan4Years",
                        "tableName": "Individuals",
                        "columnCardinality": 1,
                        "size": 17164,
                        "weight": 9.592028974654218e-7
                    },
                    {
                        "columnName": "EconomicalLevel",
                        "tableName": "Individuals",
                        "columnCardinality": 6,
                        "size": 17548,
                        "weight": 9.806625754324879e-7
                    },
                    {
                        "columnName": "Buyer",
                        "tableName": "Individuals",
                        "columnCardinality": 2,
                        "size": 31734,
                        "weight": 0.0000017734411994970693
                    },
                    {
                        "columnName": "Geography",
                        "tableName": "Individuals",
                        "columnCardinality": 21,
                        "size": 110040,
                        "weight": 0.00000614953896743737
                    },
                    {
                        "columnName": "Instruction",
                        "tableName": "Individuals",
                        "columnCardinality": 5,
                        "size": 17448,
                        "weight": 9.750741176285645e-7
                    },
                    {
                        "columnName": "Gender",
                        "tableName": "Individuals",
                        "columnCardinality": 2,
                        "size": 33470,
                        "weight": 0.0000018704568269731805
                    },
                    {
                        "columnName": "GeographyRegion",
                        "tableName": "Individuals",
                        "columnCardinality": 4,
                        "size": 49666,
                        "weight": 0.000002775563452896623
                    },
                    {
                        "columnName": "RowNumber-2662979B-1795-4F74-8F37-6A1BA8059B61",
                        "tableName": "tvBoxes",
                        "columnCardinality": 0,
                        "size": 120,
                        "weight": 6.706149364708146e-9
                    },
                    {
                        "columnName": "ID_TvBox",
                        "tableName": "tvBoxes",
                        "columnCardinality": 20809,
                        "size": 815980,
                        "weight": 0.00004560069798845461
                    },
                    {
                        "columnName": "cOD_TvBox",
                        "tableName": "tvBoxes",
                        "columnCardinality": 19045,
                        "size": 159560,
                        "weight": 0.000008916943271940265
                    },
                    {
                        "columnName": "HomeLocation",
                        "tableName": "tvBoxes",
                        "columnCardinality": 6,
                        "size": 17330,
                        "weight": 9.684797374199348e-7
                    },
                    {
                        "columnName": "RowNumber-2662979B-1795-4F74-8F37-6A1BA8059B61",
                        "tableName": "Audience",
                        "columnCardinality": 0,
                        "size": 120,
                        "weight": 6.706149364708146e-9
                    },
                    {
                        "columnName": "ID_Date",
                        "tableName": "Audience",
                        "columnCardinality": 1588,
                        "size": 83848,
                        "weight": 0.000004685810099433739
                    },
                    {
                        "columnName": "ID_Time",
                        "tableName": "Individuals",
                        "columnCardinality": 1439,
                        "size": 6493574492,
                        "weight": 0.3628906704517569
                    },
                    {
                        "columnName": "ID_Individual",
                        "tableName": "Audience",
                        "columnCardinality": 144631,
                        "size": 2221922212,
                        "weight": 0.12417118525362265
                    },
                    {
                        "columnName": "ID_Network",
                        "tableName": "Audience",
                        "columnCardinality": 240,
                        "size": 18530696,
                        "weight": 0.0010355801267333314
                    },
                    {
                        "columnName": "ID_TvBox",
                        "tableName": "Audience",
                        "columnCardinality": 20162,
                        "size": 2092405752,
                        "weight": 0.11693321253738725,
                        "isReferenced": false
                    },
                    {
                        "columnName": "weight",
                        "tableName": "Audience",
                        "columnCardinality": 1467394,
                        "size": 2648400240,
                        "weight": 0.1480047298914075
                    },
                    {
                        "columnName": "Age",
                        "tableName": "Audience",
                        "columnCardinality": 96,
                        "size": 165264896,
                        "weight": 0.009235758977657983
                    },
                    {
                        "columnName": "ID_ViewType",
                        "tableName": "Audience",
                        "columnCardinality": 3,
                        "size": 19804,
                        "weight": 0.000001106738183489001
                    },
                    {
                        "columnName": "ViewOrder",
                        "tableName": "Audience",
                        "columnCardinality": 97,
                        "size": 69972,
                        "weight": 0.00000391035569456132,
                        "isReferenced": false
                    },
                    {
                        "columnName": "weightMultipliedByAge",
                        "tableName": "Audience",
                        "columnCardinality": 9766664,
                        "size": 4100515480,
                        "weight": 0.2291555773431493
                    },
                    {
                        "columnName": "RowNumber-2662979B-1795-4F74-8F37-6A1BA8059B61",
                        "tableName": "Networks",
                        "columnCardinality": 0,
                        "size": 120,
                        "weight": 6.706149364708146e-9
                    },
                    {
                        "columnName": "ID_Network",
                        "tableName": "Networks",
                        "columnCardinality": 271,
                        "size": 8148,
                        "weight": 4.553475418636831e-7
                    },
                    {
                        "columnName": "cOD_Network",
                        "tableName": "Networks",
                        "columnCardinality": 271,
                        "size": 1664,
                        "weight": 9.29919378572863e-8
                    },
                    {
                        "columnName": "Network",
                        "tableName": "Networks",
                        "columnCardinality": 270,
                        "size": 29268,
                        "weight": 0.0000016356298300523168
                    },
                    {
                        "columnName": "Publisher",
                        "tableName": "Networks",
                        "columnCardinality": 270,
                        "size": 29268,
                        "weight": 0.0000016356298300523168
                    },
                    {
                        "columnName": "RowNumber-2662979B-1795-4F74-8F37-6A1BA8059B61",
                        "tableName": "IndividualParameters",
                        "columnCardinality": 0,
                        "size": 120,
                        "weight": 6.706149364708146e-9
                    },
                    {
                        "columnName": "ID_Individual",
                        "tableName": "IndividualParameters",
                        "columnCardinality": 148578,
                        "size": 26408656,
                        "weight": 0.0014758365971432998
                    },
                    {
                        "columnName": "ID_Date",
                        "tableName": "IndividualParameters",
                        "columnCardinality": 1589,
                        "size": 22142028,
                        "weight": 0.0012373978917129165
                    },
                    {
                        "columnName": "weight",
                        "tableName": "IndividualParameters",
                        "columnCardinality": 1560710,
                        "size": 81742640,
                        "weight": 0.004568152944213056
                    },
                    {
                        "columnName": "Age",
                        "tableName": "IndividualParameters",
                        "columnCardinality": 96,
                        "size": 3592,
                        "weight": 2.007374043169305e-7
                    },
                    {
                        "columnName": "RowNumber-2662979B-1795-4F74-8F37-6A1BA8059B61",
                        "tableName": "BridgeIndividualsTargets",
                        "columnCardinality": 0,
                        "size": 120,
                        "weight": 6.706149364708146e-9
                    },
                    {
                        "columnName": "ID_Individual",
                        "tableName": "BridgeIndividualsTargets",
                        "columnCardinality": 152275,
                        "size": 13235516,
                        "weight": 0.0007396612267915376
                    },
                    {
                        "columnName": "ID_Category",
                        "tableName": "BridgeIndividualsTargets",
                        "columnCardinality": 103,
                        "size": 3652,
                        "weight": 2.040904789992846e-7
                    },
                    {
                        "columnName": "ID_Target",
                        "tableName": "BridgeIndividualsTargets",
                        "columnCardinality": 115,
                        "size": 3812,
                        "weight": 2.130320114855621e-7
                    },
                    {
                        "columnName": "RowNumber-2662979B-1795-4F74-8F37-6A1BA8059B61",
                        "tableName": "categories",
                        "columnCardinality": 0,
                        "size": 120,
                        "weight": 6.706149364708146e-9
                    },
                    {
                        "columnName": "ID_Category",
                        "tableName": "categories",
                        "columnCardinality": 123,
                        "size": 3996,
                        "weight": 2.2331477384478128e-7
                    },
                    {
                        "columnName": "category",
                        "tableName": "categories",
                        "columnCardinality": 123,
                        "size": 24702,
                        "weight": 0.0000013804608467251718
                    },
                    {
                        "columnName": "RowNumber-2662979B-1795-4F74-8F37-6A1BA8059B61",
                        "tableName": "targets",
                        "columnCardinality": 0,
                        "size": 120,
                        "weight": 6.706149364708146e-9
                    },
                    {
                        "columnName": "ID_Target",
                        "tableName": "targets",
                        "columnCardinality": 153,
                        "size": 6452,
                        "weight": 3.6056729750914133e-7
                    },
                    {
                        "columnName": "target",
                        "tableName": "targets",
                        "columnCardinality": 153,
                        "size": 25572,
                        "weight": 0.0000014290804296193059
                    }
                ]
            },
            measures: [
                {
                    "name": "Sales Amount",
                    "tableName": "Sales",
                    "measure": "SUMX ( Sales, Sales[Quantity] * Sales[Unit Price] )",
                },
                {
                    "name": "Sales Row",
                    "tableName": "Sales",
                    "measure": "cOUNTROWS ( Sales )",
                },
                {
                    "name": "Sales Cost",
                    "tableName": "Sales",
                    "measure": "DIVIDE ( [Sales Amount] - [Sales Cost], [Sales Cost] )",
                },
                {
                    "name": "Previous",
                    "tableName": "Sales",
                    "measure": "SUMX ( Sales, ( Sales[Quantity] / RANDBETWEEN(1,6)) * Sales[Unit Price] )",
                },
                {
                    "name": "target",
                    "tableName": "Sales",
                    "measure": "AVERAGEX(Sales, Sales[Sales Amount]) + 8000",
                },
                {
                    "name": "target",
                    "tableName": "Sales",
                    "measure": "BLANK() // Expression was invalid. Check the expression override annotation",
                },
                {
                    "name": "State 1",
                    "tableName": "Sales",
                    "measure": "1",
                },
                {
                    "name": "State 2",
                    "tableName": "Sales",
                    "measure": "2",
                },
                {
                    "name": "State 3",
                    "tableName": "Sales",
                    "measure": "3",
                },
            ]
        }
    }
};