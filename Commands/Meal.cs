using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("meal", "info", null, new string[] { "food" })]
    public class Meal : ModuleBase<SocketCommandContext>
    {
        public MealService MealService { get; set; }

        [Command("meal")]
        [Alias("food")]
        public async Task MealAsync(params string[] discard)
        {
            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language)
                .texts;
            var mealCommandText = responseText.meal;
            var mealUis = mealCommandText["uis"] as Dictionary<string, object>;

            try
            {
                var response = await MealService.GetMealAsync();
                var obj = Utf8Json.JsonSerializer
                    .Deserialize<Dictionary<string, List<object>>>(response);
                var mealData = obj["meals"][0] as Dictionary<string, object>;

                var embed = new EmbedBuilder
                {
                    Color = new Color(0xfd9b3b),
                    Description = mealData["strInstructions"].ToString(),
                    Title = mealData["strMeal"].ToString(),
                    ImageUrl = mealData["strMealThumb"].ToString(),
                    Url = mealData["strYoutube"].ToString(),
                    Fields = new List<EmbedFieldBuilder>
                    {
                        {
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = mealUis["category"].ToString(),
                                Value = mealData["strCategory"].ToString()
                            }
                        },
                        {
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = mealUis["area"].ToString(),
                                Value = mealData["strArea"].ToString()
                            }
                        }
                    },
                    Footer = new EmbedFooterBuilder
                    {
                        Text = mealUis["footer"].ToString()
                    }
                };

                await Context.Channel.SendMessageAsync(mealCommandText["result"].ToString());
                await Context.Channel.SendMessageAsync(embed: embed.Build());

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                await Context.Channel.SendMessageAsync($"An error occurred: {ex.Message}");
                return;
            }
        }
    }
}
