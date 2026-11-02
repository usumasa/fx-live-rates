using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrackClubManager
{
    public sealed partial class ClubScreenController
    {
        private bool skippingAllMeetRaces;

        private void SkipAllMeetRaces(CompetitionRegistration from)
        {
            if (skippingAllMeetRaces) return;
            StartCoroutine(SkipAllMeetRacesRoutine(from));
        }

        private IEnumerator SkipAllMeetRacesRoutine(CompetitionRegistration from)
        {
            skippingAllMeetRaces = true;
            string meetId = from.MeetId ?? from.CompetitionId;
            int total = 0, completed = 0;
            foreach (var entry in session.MeetEntries(meetId, from.Year)) total += entry.Format.TotalRaces - entry.RoundRaces.Count;
            var card = OpenDialog("SkipAllRacesProgress", from.Name + " / 全レースをスキップ", 760, 320);
            var progress = Label(card, "SkipAllRacesProgressText", "結果を計算します。各レース終了時にオートセーブします。", 22, TextAnchor.MiddleCenter);
            DetailPlace(progress.rectTransform, 24, 92, 712, 148);
            string failure = null, saveFailure = null;
            try
            {
                yield return null;
                while (true)
                {
                    var next = session.NextUnfinishedMeetEvent(meetId, from.Year);
                    if (next == null) break;
                    try
                    {
                        session.RunCompetition(next.CompetitionId, next.Year, sprintRules);
                        completed++;
                        pendingLoad = null;
                    }
                    catch (Exception error) { failure = "計算で停止しました。ここまでの進行はメモリに残っています: " + error.Message; Debug.LogWarning(error.ToString()); break; }
                    // A failed autosave (e.g. mobile storage) must not stop the remaining events.
                    try { saves.Save(session.State, SaveSlot.Auto); }
                    catch (Exception error) { saveFailure = "オートセーブ失敗。進行・結果はメモリに残っています。手動保存してください: " + error.Message; Debug.LogWarning(error.Message); }
                    progress.text = RaceEvents.Name(next.Event) + " / " + completed + " / " + total + "レース完了" + (saveFailure == null ? "\nオートセーブ済み" : "\nオートセーブ失敗（計算は続行）");
                    yield return null;
                }
            }
            finally { skippingAllMeetRaces = false; }
            RefreshRosterFatigue(); RefreshUpcomingCompetition(); RefreshWeekButton();
            var state = session.State;
            weekSummary.text = string.Format("{0}     所属 {1}人 / 定員 {2}人     {3}", GameDateFormat.SeasonDate(state.Date.Year, state.Date.Week), state.Athletes.Count, state.PlayerClub.Capacity, ClubRankSummary());
            string notice = failure ?? saveFailure;
            trainingStatus.text = notice ?? (completed + "レースを計算しました。記録・経験・疲労を反映し、オートセーブ済みです。");
            NoteRankChange();
            OpenAllMeetResults(from, null, notice);
        }

        private void OpenAllMeetResults(CompetitionRegistration from, CompetitionRegistration selected = null, string message = null)
        {
            var entries = session.MeetEntries(from.MeetId ?? from.CompetitionId, from.Year);
            selected = selected ?? entries.Find(e => e.Event == from.Event) ?? from;
            int pending = entries.FindAll(e => string.IsNullOrEmpty(e.ResultRaceId)).Count;
            var lines = session.AllCompetitionResults(selected);
            bool relay = RaceEvents.IsRelay(selected.Event);
            float width, height;
            var card = OpenMeetDialog("AllResultsDialog", from.Name + " / " + (pending == 0 ? "全選手の最終結果" : "ここまでの結果"), out width, out height);
            MeetText(card, "AllResultsSummary", RaceEvents.Name(selected.Event) + " / " + lines.Count + (relay ? "チーム" : "人")
                + " / " + (pending == 0 ? "全" + entries.Count + "種目終了" : "未完了 " + pending + "種目") + " / 予選・準決勝の敗退者も表示", 20, 52, width - 40, 30, 19);
            float tabWidth = Mathf.Min(170, (width - 40) / Mathf.Max(1, entries.Count));
            for (int i = 0; i < entries.Count; i++)
            {
                var target = entries[i];
                MeetButton(card, "AllResultsEvent_" + (int)target.Event, RaceEvents.Name(target.Event), 20 + i * tabWidth, 86, tabWidth - 6, 42,
                    () => OpenAllMeetResults(from, target, message), target == selected);
            }
            var views = new List<MeetResultView>();
            foreach (var line in lines)
            {
                string reach = line.Round == 2 ? (line.Rank == 0 ? "記録なし" : selected.Format.SingleFinal ? "完了" : "決勝") : TournamentStructure.RoundName(line.Round) + "敗退";
                string qualification = line.Own ? session.AdvancementText(selected, line.ParticipantId, calendar) : "";
                if (qualification.Length > 0) reach += " / " + qualification;
                views.Add(new MeetResultView {
                    Name = line.Name, Club = line.ClubName,
                    Rank = line.Round == 2 ? (line.Rank == 0 ? "—" : line.Rank + "位") : TournamentStructure.RoundName(line.Round) + "敗退",
                    Mark = line.LastMark > 0 ? RaceEvents.MarkText(selected.Event, line.LastMark) : "—",
                    PB = line.PB > 0 ? RecordText(line.PB, line.FirstPB, line.NewPB, false, "PB", selected.Event) : "—",
                    SB = line.SB > 0 ? RecordText(line.SB, line.FirstSB, line.NewSB, false, "SB", selected.Event) : "—",
                    HighlightPB = line.FirstPB || line.NewPB, HighlightSB = line.FirstSB || line.NewSB,
                    Own = line.Own, Winner = line.Round == 2 && line.Rank == 1, Status = reach,
                    Experience = "+" + line.Experience + (relay ? "XP/人" : "XP") + (line.AbilityGrowth > 0 ? " 成長+" + line.AbilityGrowth.ToString("0.00") : ""),
                    Extra = relay ? string.Join(" / ", line.Members.ToArray()) : ""
                });
            }
            BuildMeetResultTable(card, views, width, height, 136, true, "この種目に自クラブの結果はありません。");
            if (!string.IsNullOrEmpty(message)) MeetText(card, "AllResultsNotice", message, 20, height - 94, width - 40, 24, 16);
            float bw = (width - 64) / 4, y = height - 66;
            var chosen = selected;
            MeetButton(card, "AllResultsOwn", "自クラブの成績", 20, y, bw, 54, () => OpenOwnResults(chosen));
            MeetButton(card, "AllResultsRounds", "ラウンド別・リプレイ", 28 + bw, y, bw, 54, () => OpenTournament(chosen));
            MeetButton(card, "RaceExperienceDetails", "経験・成長の内訳", 36 + bw * 2, y, bw, 54, () => OpenRaceExperience(null, chosen, () => OpenAllMeetResults(from, chosen, message)));
            MeetButton(card, "AllResultsToManagement", "管理画面に戻る", 44 + bw * 3, y, bw, 54, () => { CloseDialog(); RefreshWeekButton(); }, true);
            ShowAbilityCapNotice();
        }
    }
}
