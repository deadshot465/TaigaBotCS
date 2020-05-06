using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TaigaBotCS.Commands
{
    [Group("minigame")]
    [Attributes.Command("minigame", "fun", null, null)]
    public class MiniGame : ModuleBase<SocketCommandContext>
    {
        [Command("tictactoe")]
        public async Task TicTacToeAsync()
        {

        }
    }
}
