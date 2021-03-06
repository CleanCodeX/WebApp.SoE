﻿using System;
using System.Drawing;
using System.Threading.Tasks;
using Common.Shared.Min.Extensions;
using Microsoft.AspNetCore.Components;
using WebApp.SoE.Extensions;
using WebApp.SoE.Helpers;
using WebApp.SoE.Services;

namespace WebApp.SoE.Pages.Bases
{
	public abstract class AutoLangContentIdMarkupBase : LangContentIdMarkupBase
	{
		protected enum TranslateOptionEnum
		{
			Markup,
			Html
		}

		private const int TranslateErrorDelayInMilliseconds = 200;
		private static readonly TranslateOptionEnum TranslateOption = TranslateOptionEnum.Markup;

		[Inject] private ITranslator Translator { get; set; } = default!;

		private bool _autoTranslate = false; 

		protected bool AutoTranslate
		{
			get => _autoTranslate;
			set
			{
				if (_autoTranslate == value) return;

				_autoTranslate = value;

				LoadContentAsync().ContinueWith(_ => InvokeAsync(StateHasChanged));
			}
		}

		protected bool ShowAutoTranslateOption => ShouldBeTranslated && LangFileFound == false;

		protected override async Task<MarkupString> ParseContentAsync(string? content)
		{
			if (!AutoTranslate || LangFileFound == true) return await base.ParseContentAsync(content);

			MarkupString markup;

			if (TranslateOption == TranslateOptionEnum.Markup)
			{
				content = await TranslateContent(content!) ?? content;
				markup = await base.ParseContentAsync(content);
			}
			else
			{
				markup = await base.ParseContentAsync(content);
				markup = (await TranslateContent(markup.Value) ?? content)!.ToMarkup();
			}

			return markup;
		}

		private async Task<string?> TranslateContent(string content)
		{
			try
			{
				return ContentCorrections(await Translator.TranslateTextAsync(PrefixContent(content), "en", Language));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				await Task.Delay(TranslateErrorDelayInMilliseconds);
				return content;
			}
		}

		private string PrefixContent(string content)
		{
			var textTranslationSuffix = $"This text has been automatically translated. [{Language}]".ColorText(Color.DarkGray) + Environment.NewLine.Repeat(2);

			return textTranslationSuffix + content;
		}

		private static string? ContentCorrections(string? content)
		{
			if(content is null) return null;

			return content.FixAutoTranslatedText();
		}
	}
}