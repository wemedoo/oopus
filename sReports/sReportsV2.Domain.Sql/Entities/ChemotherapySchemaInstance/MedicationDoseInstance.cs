using sReportsV2.Domain.Sql.Entities.ChemotherapySchema;
using sReportsV2.Domain.Sql.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace sReportsV2.Domain.Sql.Entities.ChemotherapySchemaInstance
{
    [Table("MedicationDoseInstances")]
    public class MedicationDoseInstance : IEditChildEntries<MedicationDoseTimeInstance>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [Column("MedicationDoseInstanceId")]
        public int MedicationDoseInstanceId { get; set; }
        public int DayNumber { get; set; }
        public DateTime? Date { get; set; }
        public int? IntervalId { get; set; }
        public List<MedicationDoseTimeInstance> MedicationDoseTimes { get; set; } = new List<MedicationDoseTimeInstance>();
        public bool IsDeleted { get; set; }
        public int MedicationInstanceId { get; set; }
        public int? UnitId { get; set; }
        [ForeignKey("UnitId")]
        public virtual Unit Unit { get; set; }

        public string GetStartTime()
        {
            if (MedicationDoseTimes.Count == 0 || MedicationDoseTimes.Where(t => !t.IsDeleted).FirstOrDefault() == null) return "";

            return MedicationDoseTimes.Where(t => !t.IsDeleted).FirstOrDefault()?.Time;
        }

        public void Copy(MedicationDoseInstance medicationDose)
        {
            this.DayNumber = medicationDose.DayNumber;
            this.Date = medicationDose.Date;
            this.IntervalId = medicationDose.IntervalId;
            this.UnitId = medicationDose.UnitId;

            CopyEntries(medicationDose.MedicationDoseTimes);
        }

        public void CopyEntries(List<MedicationDoseTimeInstance> upcomingEntries)
        {
            if (upcomingEntries != null)
            {
                DeleteExistingRemovedEntries(upcomingEntries);
                AddNewOrUpdateOldEntries(upcomingEntries);
            }
        }

        public void DeleteExistingRemovedEntries(List<MedicationDoseTimeInstance> upcomingEntries)
        {
            foreach (var medicationDoseTime in MedicationDoseTimes)
            {
                var remainingDoseTime = upcomingEntries.Any(x => x.MedicationDoseTimeInstanceId == medicationDoseTime.MedicationDoseTimeInstanceId);
                if (!remainingDoseTime)
                {
                    medicationDoseTime.IsDeleted = true;
                    //TODO: here set last update in case this entity will gain LastUpdate property
                }
            }
        }

        public void AddNewOrUpdateOldEntries(List<MedicationDoseTimeInstance> upcomingEntries)
        {
            foreach (var doseTime in upcomingEntries)
            {
                if (doseTime.MedicationDoseTimeInstanceId == 0)
                {
                    MedicationDoseTimes.Add(doseTime);
                }
                else
                {
                    var dbDoseTime = MedicationDoseTimes.FirstOrDefault(x => x.MedicationDoseTimeInstanceId == doseTime.MedicationDoseTimeInstanceId && !x.IsDeleted);
                    if (dbDoseTime != null)
                    {
                        dbDoseTime.Copy(doseTime);
                    }
                }
            }
        }
    }
}
