using sReportsV2.Domain.Sql.Entities.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace sReportsV2.Domain.Sql.Entities.ChemotherapySchema
{
    public class MedicationDose : IEditChildEntries<MedicationDoseTime>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [Column("MedicationDoseId")]
        public int MedicationDoseId { get; set; }
        public int DayNumber { get; set; }

        public List<MedicationDoseTime> MedicationDoseTimes { get; set; } = new List<MedicationDoseTime>();
        public bool IsDeleted { get; set; }
        public int? IntervalId { get; set; }
        public int? UnitId { get; set; }
        [ForeignKey("UnitId")]
        public virtual Unit Unit { get; set; }
        public int MedicationId { get; set; }

        public string GetStartTime()
        {
            if (MedicationDoseTimes.Count == 0 || MedicationDoseTimes.Where(t => !t.IsDeleted).FirstOrDefault() == null) return "";

            return MedicationDoseTimes.Where(t => !t.IsDeleted).FirstOrDefault()?.Time;
        }

        public void Copy(MedicationDose medicationDose)
        {
            this.DayNumber = medicationDose.DayNumber;
            this.IntervalId = medicationDose.IntervalId;
            this.UnitId = medicationDose.UnitId;

            CopyEntries(medicationDose.MedicationDoseTimes);
        }

        public void CopyEntries(List<MedicationDoseTime> upcomingEntries)
        {
            if (upcomingEntries != null)
            {
                DeleteExistingRemovedEntries(upcomingEntries);
                AddNewOrUpdateOldEntries(upcomingEntries);
            }
        }

        public void DeleteExistingRemovedEntries(List<MedicationDoseTime> upcomingEntries)
        {
            foreach (var medicationDoseTime in MedicationDoseTimes)
            {
                var remainingDoseTime = upcomingEntries.Any(x => x.MedicationDoseTimeId == medicationDoseTime.MedicationDoseTimeId);
                if (!remainingDoseTime)
                {
                    medicationDoseTime.IsDeleted = true;
                    //TODO: here set last update in case this entity will gain LastUpdate property
                }
            }
        }

        public void AddNewOrUpdateOldEntries(List<MedicationDoseTime> upcomingEntries)
        {
            foreach (var doseTime in upcomingEntries)
            {
                if (doseTime.MedicationDoseTimeId == 0)
                {
                    MedicationDoseTimes.Add(doseTime);
                }
                else
                {
                    var dbDoseTime = MedicationDoseTimes.FirstOrDefault(x => x.MedicationDoseTimeId == doseTime.MedicationDoseTimeId && !x.IsDeleted);
                    if (dbDoseTime != null)
                    {
                        dbDoseTime.Copy(doseTime);
                    }
                }
            }
        }
    }
}
