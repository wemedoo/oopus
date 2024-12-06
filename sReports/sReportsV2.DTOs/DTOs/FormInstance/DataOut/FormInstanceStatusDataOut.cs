﻿using sReportsV2.Common.Enums;
using sReportsV2.DTOs.DTOs.Form.DataOut;
using sReportsV2.DTOs.User.DataOut;
using System;

namespace sReportsV2.DTOs.DTOs.FormInstance.DataOut
{
    public class FormInstanceStatusDataOut : FormStatusAbstractDataOut
    {
        public int CreatedById { get; set; }
        public UserShortInfoDataOut CreatedBy { get; set; }
        public string CreatedByActiveOrganization { get; set; }
        public DateTime CreatedOn { get; set; }
        public FormState FormInstanceStatus { get; set; }
        public bool IsSigned { get; set; }

        public override dynamic StatusValue
        {
            get
            {
                return FormInstanceStatus;
            }
        }

        public override DateTime CreatedDateTime
        {
            get
            {
                return CreatedOn;
            }
        }

        public override string CreatedName
        {
            get
            {
                string name = string.Empty;
                if (CreatedBy != null)
                {
                    name = CreatedBy.Name;
                }
                return name;
            }
        }
    }
}
