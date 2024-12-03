﻿using sReportsV2.DTOs.Common.DTO;
using System;
using System.ComponentModel.DataAnnotations;
using sReportsV2.Common.Constants;

namespace sReportsV2.DTOs.CustomAttributes
{
    public class DateRangeAttribute : ValidationAttribute 
    {
        public string GetErrorMessage() => $"Period end date cannot be before Period start date.";

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value != null)
            {
                var period = (PeriodDTO)validationContext.ObjectInstance;
                var startDate = period.StartDate;
                var endDate = (DateTime)value;
                if(startDate > endDate)
                {
                    return new ValidationResult(GetErrorMessage());
                }
            }

            return ValidationResult.Success;
        }
    }
}
