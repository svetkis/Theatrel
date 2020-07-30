using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using theatrel.TLBot.Interfaces;

namespace theatrel.DataAccess.Entities
{
    public class ChatDataInfo : IChatDataInfo
    {
        [Key]
        public long ChatId { get; set; }

        public string Culture { get; set; }

        public int CurrentStepId { get; set; }
        public int PreviousStepId { get; set; }

        public DateTime When { get; set; }

        [NotMapped]
        public DayOfWeek[] Days
        {
            get => DbDays?.Split(',').Select(d => (DayOfWeek) int.Parse(d)).ToArray();
            set => DbDays = value != null ? string.Join(",", value.Select(d => ((int)d).ToString())) : null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string DbDays { get; set; }

        [NotMapped]
        public string[] Types
        {
            get => DbTypes?.Split(',').ToArray();
            set => DbTypes = value != null ? string.Join(",", value) : null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string DbTypes { get; set; }

        public DateTime LastMessage { get; set; }
        public DialogStateEnum DialogState { get; set; }

        public void Clear()
        {
            CurrentStepId = 0;
            PreviousStepId = -1;

            Days = null;
            Types = null;
            DialogState = DialogStateEnum.DialogStarted;

            LastMessage = new DateTime();
        }
    }
}
