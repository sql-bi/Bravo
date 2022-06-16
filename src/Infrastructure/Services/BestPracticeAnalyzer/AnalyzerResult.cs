namespace Sqlbi.Bravo.Infrastructure.Services.BestPracticeAnalyzer
{
    using TabularEditor.TOMWrapper;

    internal class AnalyzerResult
    {
        public bool RuleEnabled { get; set; } = true;

        public bool RuleHasError => RuleError is not null;

        public bool InvalidCompatibilityLevel { get; set; } = false;

        public string? RuleError { get; set; }

        public RuleScope RuleErrorScope { get; set; }

        public string? ObjectType => RuleHasError ? "Error" : Object?.GetTypeName();

        public string? ObjectName
        {
            get
            {
                if (Object == null) 
                    return string.Empty;

                if (RuleHasError) 
                    return RuleError;

                if (Object is KPI kpi) 
                    return kpi.Measure.DaxObjectFullName + ".KPI";

                return (Object as IDaxObject)?.DaxObjectFullName ?? Object.Name;
            }
        }

        public string? RuleName => Rule?.Name;

        public ITabularNamedObject? Object { get; set; }

        public BestPracticeRule? Rule { get; set; }

        public bool CanFix => Rule?.FixExpression != null;

        /// <summary>
        /// Indicates whether this rule should be ignored on this particular object
        /// </summary>
        public static bool Ignored
        {
            get
            {
                //var obj = Object as IAnnotationObject;
                //if (obj != null)
                //{
                //    var air = new AnalyzerIgnoreRules(obj);
                //    return air.RuleIDs.Contains(Rule.ID);
                //}
                return false;
            }
        }
    }
}
