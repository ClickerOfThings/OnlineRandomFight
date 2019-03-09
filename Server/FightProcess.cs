using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    static class FightProcess
    {
        delegate void SendToAllDelegate(string x);
        static readonly SendToAllDelegate SendToAll = Server.SendToAll; // я ленивый
        static List<Fighter> fighters = new List<Fighter>();
        static Random rand = new Random();
        private static int delay = 7000;
        public static readonly int MIN_DELAY = 500;
        public static int Delay
        {
            get => delay;
            set
            {
                if (value > int.MaxValue)
                {
                    delay = int.MaxValue;
                }
                else if (value <= MIN_DELAY)
                {
                    delay = MIN_DELAY;
                }
                else
                {
                    delay = value;
                }
            }
        }
        public static bool Start()
        {
            foreach (Client client in Server.players)
            {
                fighters.Add(client.fighter);
            }
            SendToAll("Игра скоро начнется...");
            Fighter.InitializeSettings();
            foreach (Fighter fighter in fighters)
            {
                fighter.InitializeFighter();
            }
            Thread.Sleep(2000);
            SendToAll("erase|debug_erase");
            SendToAll("notification|debug_notification");
            Thread.Sleep(500);
            SendToAll("Сегодня на арене сталкиваются два бойца.");
            Thread.Sleep(1000);
            SendToAll(string.Format("{0} и {1}", fighters[0].fighterName, fighters[1].fighterName));
            Thread.Sleep(1000);
            SendToAll("Посмотрим же, кто сильнее!");
            Thread.Sleep(1000);
            Preparing();
            return EndGame();
        }

        public static void ReInitalizeAll()
        {
            fighters.Clear();
        }

        static void Preparing()
        {
            foreach (Fighter fighter in fighters)
            {
                SendToAll(string.Format("Здоровья у {0} - {1}", fighter.fighterName, fighter.Health));
                SendToAll(string.Format("Сильный навык у {0} - {1}", fighter.fighterName, fighter.StrongAbility.ToString()));
            }
            Thread.Sleep(1000);
            SendToAll("Да начнется бой!");
            Thread.Sleep(1000);
            currentTurn = fighters[rand.Next(2)];
            notCurrentTurn = fighters.Find(x => x != currentTurn);
            SendToAll(string.Format("Первым начинает {0}", currentTurn.fighterName));
            Thread.Sleep(1000);
            Fighting();
            return;
        }
        static Fighter currentTurn;
        static Fighter notCurrentTurn;
        enum Turn
        {
            Hit,
            Heal,
            HitAndHeal,
            Nothing
        }
        static List<Turn> turns = Enum.GetValues(typeof(Turn)).Cast<Turn>().ToList();
        static List<Turn> turnsNoHeal = new List<Turn>(turns); // массив без лечения
        static Turn randomTurn;
        static bool endFight = false;
        static void Fighting()
        {
            turnsNoHeal.Remove(Turn.Heal); // убираем лечение
            turnsNoHeal.Remove(Turn.HitAndHeal); // убираем удар и лечение
            while (!endFight)
            {
                SendToAll("erase|debug_erase");
                if (currentTurn.Health < Server.MaxHealthSet - (Server.MaxHealthSet*0.25))
                {
                    randomTurn = turns[rand.Next(turns.Count)];
                }
                else
                {
                    randomTurn = turnsNoHeal[rand.Next(turnsNoHeal.Count)];
                }
                //Turn randomTurn = (Turn)values.GetValue(rand.Next(values.Length));
                switch (randomTurn)
                {
                    case Turn.Hit:
                        if (HitTurn())
                        {
                            endFight = true;
                        }
                        break;
                    case Turn.Heal:
                        HealTurn();
                        break;
                    case Turn.HitAndHeal:
                        if (HitAndHealTurn())
                        {
                            endFight = true;
                        }
                        break;
                    case Turn.Nothing:
                        NothingTurn();
                        break;
                }
                string state = "";
                foreach(Fighter player in fighters)
                {
                    state += string.Format("ЗДР {0}: {1} СПБ: {2}\n",
                    player.fighterName,
                    player.Health,
                    player.StrongAbility);
                }
                SendToAll(state);
                Fighter temp = currentTurn;
                currentTurn = notCurrentTurn;
                notCurrentTurn = temp;
                Thread.Sleep(delay);
            }
            return;
        }

        static bool EndGame() // true если реванш, false если что-то пошло не то
        {
            endFight = false;
            SendToAll("erase|debug_erase");
            SendToAll("Конец игры!!");
            Thread.Sleep(2000);
            SendToAll("Итоги:");
            Thread.Sleep(1000);
            SendToAll(string.Format("Здоровье {0} - {1}",
                fighters[0].fighterName,
                fighters[0].Health));
            Thread.Sleep(500);
            SendToAll(string.Format("Здоровье {0} - {1}",
                fighters[1].fighterName,
                fighters[1].Health));
            Thread.Sleep(2000);
            Fighter winner = GetMostHealth(fighters.ToArray());
            SendToAll(string.Format("Похоже, что {0} выиграл. Поздравляем!", winner.fighterName));
            SendToAll("Еще раз?");
            List<Task> tasks = new List<Task>();
            foreach (Client user in Server.players)
            {
                tasks.Add(Task.Run(() =>
                {
                    user.SendToClient("replay|debug_replay");
                    string get = user.GetMessage();
                    if (get != null)
                    {
                        if (get.ToLower().Split('|')[0] == "replay_y")
                        {
                            user.SendToClient("Ожидаем выбор второго игрока...");
                            return;
                        }
                        else if (get.ToLower().Split('|')[0] == "replay_n")
                        {
                            user.SendToClient("Вы отказались от реванша, отключение от сервера...");
                            user.CloseConnection();
                            throw new Exception();
                        }
                        else
                        {
                            user.CloseConnection();
                            throw new Exception();
                        }
                    }
                    else
                    {
                        throw new Exception();
                    }
                }));
            }
            Task waitforreplay = Task.WhenAll(tasks);
            try
            {
                waitforreplay.Wait();
                return true;
            }
            catch
            {
                return false;
            }
        }

        static Fighter GetMostHealth(Fighter[] array)
        {
            Fighter result = array[0];
            int comparingHealth = 0;
            foreach (Fighter fighter in array)
            {
                if (comparingHealth < fighter.Health)
                {
                    comparingHealth = fighter.Health;
                    result = fighter;
                }
            }
            return result;
        }

        static bool HitTurn()
        {
            SendToAll(Hit());
            if (notCurrentTurn.Health <= 0) return true;
            else return false;
        }

        static void HealTurn()
        {
            SendToAll(Heal());
        }

        static bool HitAndHealTurn()
        {
            string message = Hit() + " И " + Heal();
            SendToAll(message);
            if (notCurrentTurn.Health < 0) return true;
            else return false;
        }

        static void NothingTurn()
        {
            SendToAll(string.Format("{0} упустил возможность ударить {1}",
                currentTurn.fighterName,
                notCurrentTurn.fighterName));
        }

        static string Hit() // true - конец игры, false - нет
        {
            int hit_damage = rand.Next(1, 41);
            hit_damage += currentTurn.StrongAbility == Fighter.Ability.Strength ? rand.Next(5, 26) : 0;
            int dodge_chance = rand.Next(1, 11);
            dodge_chance += notCurrentTurn.StrongAbility == Fighter.Ability.Dodge ? rand.Next(5, 11) : 0;
            string message;
            if (rand.Next(101) > dodge_chance) // не увернулся
            {
                notCurrentTurn.Health -= hit_damage;
                message = string.Format("{0} ударил {1} на {2} единиц урона",
                    currentTurn.fighterName,
                    notCurrentTurn.fighterName,
                    hit_damage);
            }
            else // увернулся
            {
                message = string.Format("{0} увернулся от удара {1}",
                    notCurrentTurn.fighterName,
                    currentTurn.fighterName);
            }
            return message;
        }

        static string Heal()
        {
            int healrate = rand.Next(3, 46);
            currentTurn.Health += healrate;
            string message = string.Format("{0} подлечился на {1} здоровья", currentTurn.fighterName, healrate);
            return message;
        }
    }
}
