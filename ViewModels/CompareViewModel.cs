﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using Common.Shared.Min.Extensions;
using Common.Shared.Min.Helpers;
using Microsoft.AspNetCore.Components;
using SramComparer.Helpers;
using SramComparer.Server.Extensions;
using SramComparer.Server.Services;
using SramComparer.Server.Shared;
using SramComparer.Server.Shared.Enums;
using SramComparer.Server.ViewModels.Bases;
using SramComparer.Services;
using SramComparer.SoE.Enums;
using SramComparer.SoE.Services;
using Res = SramComparer.Server.Properties.Resources;
#pragma warning disable 8509

namespace SramComparer.Server.ViewModels
{
	/// <summary>Viewmodel for SoE SRAM comparison</summary>
	public class CompareViewModel : ViewModelBase
	{
		public GameId ComparisonGame { get; set; }
		public MemoryStream? ComparisonFileStream { get; set; }
		public MarkupString OutputMessage { get; set; }
		public bool IsComparing { get; set; }
		public bool CanCompare => !IsComparing && CurrentFileStream is not null && ComparisonFileStream is not null;
		public ComparisonFlagsSoE Flags { get; set; }
		public bool UseColoredOutput { get; set; } = true;

		public bool WholeGameBuffer
		{
			get => Options.Flags.HasFlag(ComparisonFlagsSoE.WholeGameBuffer);
			set => Options.Flags = (ComparisonFlagsSoE)EnumHelper.InvertUIntFlag(Options.Flags, ComparisonFlagsSoE.WholeGameBuffer);
		}

		public bool NonGameBuffer
		{
			get => Options.Flags.HasFlag(ComparisonFlagsSoE.NonGameBuffer);
			set => Options.Flags = (ComparisonFlagsSoE)EnumHelper.InvertUIntFlag(Options.Flags, ComparisonFlagsSoE.NonGameBuffer);
		}

		public enum AllSingleFlag : uint
		{
			[Display(Name = nameof(Res.DontShow), ResourceType = typeof(Res))]
			None,
			[Display(Name = nameof(Res.AffectedGamesOnly), ResourceType = typeof(Res))]
			AffectedGamesOnly,
			[Display(Name = nameof(Res.AllGames), ResourceType = typeof(Res))]
			AllGames
		}

		public AllSingleFlag GameChecksum { get; set; }
		public AllSingleFlag Unknown12B { get; set; }

		public void Compare()
		{
			try
			{
				CanCompare.ThrowIfFalse(nameof(CanCompare));

				IsComparing = true;

				SetOptions();

				using var output = new StringWriter { NewLine = "<br>" };

				Requires.NotNull(CurrentFileStream, nameof(CurrentFileStream));
				Requires.NotNull(ComparisonFileStream, nameof(ComparisonFileStream));

				CurrentFileStream.Position = 0;
				ComparisonFileStream.Position = 0;

				new CommandHandlerSoE(UseColoredOutput ? new HtmlConsolePrinterSoE() : new ConsolePrinter()).Compare(CurrentFileStream, ComparisonFileStream, Options, output);

				OutputMessage = output.ToString().ToMarkup();
			}
			catch (Exception ex)
			{
				OutputMessage = ex.Message.ColorText(Color.Red).ToMarkup();
			}

			IsComparing = false;
		}

		private void SetOptions()
		{
			Options.Region = Region;
			Options.CurrentGame = CurrentGame.ToInt();
			Options.ComparisonGame = ComparisonGame.ToInt();

			Options.Flags = Options.Flags & ~ComparisonFlagsSoE.AllGameChecksums;
			if (GameChecksum != default)
				Options.Flags = GameChecksum switch
				{
					AllSingleFlag.AllGames => Options.Flags |= ComparisonFlagsSoE.AllGameChecksums,
					AllSingleFlag.AffectedGamesOnly => Options.Flags |= ComparisonFlagsSoE.GameChecksum,
				};

			Options.Flags = Options.Flags & ~ComparisonFlagsSoE.AllUnknown12Bs;
			if (Unknown12B != default)
				Options.Flags = Unknown12B switch
				{
					AllSingleFlag.AllGames => Options.Flags |= ComparisonFlagsSoE.AllUnknown12Bs,
					AllSingleFlag.AffectedGamesOnly => Options.Flags |= ComparisonFlagsSoE.Unknown12B,
				};
		}
	}
}