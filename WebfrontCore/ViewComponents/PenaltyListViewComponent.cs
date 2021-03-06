﻿using Microsoft.AspNetCore.Mvc;
using SharedLibraryCore;
using SharedLibraryCore.Dtos;
using SharedLibraryCore.Objects;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebfrontCore.ViewComponents
{
    public class PenaltyListViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(int offset, Penalty.PenaltyType showOnly)
        {
            var penalties = await Program.Manager.GetPenaltyService().GetRecentPenalties(12, offset, showOnly);
            var penaltiesDto = penalties.Select(p => new PenaltyInfo()
            {
                Id = p.PenaltyId,
                OffenderId = p.OffenderId,
                OffenderName = p.Offender.Name,
                PunisherId = p.PunisherId,
                PunisherName = p.Punisher.Name,
                PunisherLevel = p.Punisher.Level.ToLocalizedLevelName(),
                PunisherLevelId = (int)p.Punisher.Level,
#if DEBUG
                Offense = !string.IsNullOrEmpty(p.AutomatedOffense) ? p.AutomatedOffense : p.Offense,
#else
                Offense = User.Identity.IsAuthenticated && !string.IsNullOrEmpty(p.AutomatedOffense) ? p.AutomatedOffense : p.Offense,
#endif
                Type = p.Type.ToString(),
                TimePunished = Utilities.GetTimePassed(p.When, false),
                // show time passed if ban
                TimeRemaining = DateTime.UtcNow > p.Expires ? "" : $"{((p.Expires ?? DateTime.MaxValue).Year == DateTime.MaxValue.Year ? Utilities.GetTimePassed(p.When, true) : Utilities.TimeSpanText((p.Expires ?? DateTime.MaxValue) - DateTime.UtcNow))}",
                Sensitive = p.Type == Penalty.PenaltyType.Flag,
                AutomatedOffense = p.AutomatedOffense
            });

#if DEBUG
            penaltiesDto = penaltiesDto.ToList();
#else
            penaltiesDto = User.Identity.IsAuthenticated ? penaltiesDto.ToList() : penaltiesDto.Where(p => !p.Sensitive).ToList();
#endif

            return View("_List", penaltiesDto);
        }
    }
}
