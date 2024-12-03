using sReportsV2.Domain.Entities.CustomFieldFilters;
using System.Collections.Generic;

namespace sReportsV2.DTOs.DTOs.Field.DataOut.Custom
{
    public class CustomFieldFilterDataOut
    {
        public string CustomFieldFiltersId { get; set; }
        public List<CustomFieldFilterData> CustomFieldFiltersData { get; set; }
        public string FormDefinitonId { get; set; }
        public string OverallOperator { get; set; }

        public string RenderCustomFilterText()
        {
            string result = "";

            for (int i = 0; i < CustomFieldFiltersData.Count; i++)
            {
                result += $"{CustomFieldFiltersData[i].FieldLabel} is {CustomFieldFiltersData[i].FilterOperator} to {CustomFieldFiltersData[i].Value}";
                if (i < CustomFieldFiltersData.Count - 1)
                    result += $" {OverallOperator.ToUpper()} ";
            }
            return result;
        }
    }

    
}
