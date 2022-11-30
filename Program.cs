using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using SFML.Audio;

namespace _8_Snake
{
    public class Program
    {
        public static void Main()
        {
            var game = new Game(Console.WindowWidth, Console.WindowHeight);
            game.Start();
        }
    }

    public enum Direction
    {
        UP,
        DOWN,
        LEFT,
        RIGHT,
    }

    public class GamePoint
    {
        public int w;
        public int h;
        public byte mark;

        public GamePoint(int w, int h, byte mark)
        {
            this.w = w;
            this.h = h;
            this.mark = mark;
        }
    }

    public class Game
    {
        private readonly char[,] screen;
        private readonly int screenWidth;
        private readonly int screenHeight;

        private readonly int fieldBorder;

        private readonly byte[,] field;
        private readonly int fieldWidth;
        private readonly int fieldHeight;
        private const byte fieldEmptyMark = 0;
        private const byte fieldFoodMark = 1;
        private const byte fieldSnakeBodyMark = 2;
        private const byte fieldSnakeHeadMark = 3;

        private int score;
        private int scoreMax;

        private const char snakeBodyView = '█';
        private const char snakeHeadView = '▓';
        private const char foodView = '░';
        private List<GamePoint> snake;
        private GamePoint food;

        private readonly Random rnd = new Random();

        private readonly Music playerMusic;
        private readonly Sound playerSoundCollect;
        private readonly Sound playerSoundGameOver;

        public Game(int screenWidth, int screenHeight)
        {
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            screen = new char[screenWidth, screenHeight];

            fieldBorder = screenHeight * 2;

            fieldWidth = screenHeight;
            fieldHeight = screenHeight;
            field = new byte[fieldWidth, fieldHeight];

            Console.CursorVisible = false;
            Console.Title = "Slither.io";

            var dir = Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName;

            var musicLocation = $"{dir}/assets/tetris.wav";
            playerMusic = new Music(musicLocation) { Loop = true, Volume = 30, };

            var soundLocation = $"{dir}/assets/battle-city-sfx-10.wav";
            playerSoundCollect = new Sound(new SoundBuffer(soundLocation)) { Volume = 50, };

            var soundLocation2 = $"{dir}/assets/battle-city-sfx-11.wav";
            playerSoundGameOver = new Sound(new SoundBuffer(soundLocation2)) { Volume = 50, };

            Init();
        }

        public void Start()
        {
            playerMusic.Play();

            var dir = Direction.RIGHT;
            var isGameOver = false;
            var isExit = false;

            while (!isExit)
            {
                while (!isGameOver && !isExit)
                {
                    ConsoleKeyInfo key;
                    if (Console.KeyAvailable)
                    {
                        key = Console.ReadKey(false);

                        switch (key.Key)
                        {
                            case ConsoleKey.UpArrow:
                                dir = Direction.UP;
                                break;
                            case ConsoleKey.DownArrow:
                                dir = Direction.DOWN;
                                break;
                            case ConsoleKey.LeftArrow:
                                dir = Direction.LEFT;
                                break;
                            case ConsoleKey.RightArrow:
                                dir = Direction.RIGHT;
                                break;
                            case ConsoleKey.Escape:
                                isExit = true;
                                break;
                            case ConsoleKey.C:
                                Console.CursorVisible = !Console.CursorVisible;
                                break;
                        }
                    }

                    MoveSnake(dir);

                    if (DetectSnakeEat())
                    {
                        playerSoundCollect.Play();
                        GrowSnake();
                        MakeNewFood();
                    }

                    isGameOver = DetectSelfEact();
                    if (isGameOver)
                        playerSoundGameOver.Play();

                    FillField();
                    FillScreenField();
                    FillScreenInfo();
                    DrawScreen();

                    Thread.Sleep(20);
                }

                Console.Clear();

                Console.SetCursorPosition(screenWidth / 2 - 10, screenHeight / 2 - 1);
                Console.WriteLine("Game over!");

                Console.SetCursorPosition(screenWidth / 2 - 10, screenHeight / 2);
                Console.WriteLine("Press R to restart!");

                Console.SetCursorPosition(screenWidth / 2 - 10, screenHeight / 2 + 1);
                Console.WriteLine("Press ESC to exit!");

                var key2 = Console.ReadKey(false);

                if (key2.Key == ConsoleKey.R)
                {
                    isGameOver = false;
                    isExit = false;
                    Init();
                }
                if (key2.Key == ConsoleKey.Escape)
                {
                    isExit = true;
                    Init();
                }
            }

            playerMusic.Dispose();
        }

        private void Init()
        {
            snake = new List<GamePoint> {
                new GamePoint(6, 5, fieldSnakeHeadMark),
                new GamePoint(5, 5, fieldSnakeBodyMark),
                new GamePoint(4, 5, fieldSnakeBodyMark),
                new GamePoint(3, 5, fieldSnakeBodyMark),
                new GamePoint(2, 5, fieldSnakeBodyMark),
            };

            food = new GamePoint(fieldWidth - 5, fieldHeight - 5, fieldFoodMark);

            score = 0;
        }

        private bool DetectSelfEact()
        {
            return snake.Any(x =>
            {
                return x.mark != fieldSnakeHeadMark && snake[0].w == x.w && snake[0].h == x.h;
            });
        }

        private bool DetectSnakeEat()
        {
            return snake[0].w == food.w && snake[0].h == food.h;
        }

        private void GrowSnake()
        {
            var w = snake[snake.Count - 1].w;
            var h = snake[snake.Count - 1].h;

            snake.Add(new GamePoint(w, h, fieldSnakeBodyMark));

            score++;

            if (score > scoreMax)
                scoreMax = score;
        }

        private void MakeNewFood()
        {
            var list = new List<GamePoint>();

            for (var w = 0; w < field.GetLength(0); w++)
            {
                for (var h = 0; h < field.GetLength(1); h++)
                {
                    if (food.w == w && food.h == h || snake.Any((x) => { return x.w == w && x.h == h; }))
                        continue;

                    list.Add(new GamePoint(w, h, fieldEmptyMark));
                }
            }

            var next = rnd.Next(0, list.Count);

            food.w = list[next].w;
            food.h = list[next].h;
        }

        private void MoveSnake(Direction dir)
        {
            int secondBlockW = 0;
            int secondBlockH = 0;

            for (var i = snake.Count - 1; i > -1; i--)
            {
                switch (snake[i].mark)
                {
                    case fieldSnakeHeadMark:
                        if (dir == Direction.UP && (snake[i].h - 1) != secondBlockH)
                            snake[i].h--;
                        else if (dir == Direction.DOWN && (snake[i].h + 1) != secondBlockH)
                            snake[i].h++;
                        else if (dir == Direction.LEFT && (snake[i].w - 1) != secondBlockW)
                            snake[i].w--;
                        else if (dir == Direction.RIGHT && (snake[i].w + 1) != secondBlockW)
                            snake[i].w++;
                        else
                        {
                            if (snake[i].w == secondBlockW)
                                snake[i].w++;
                            if (snake[i].h == secondBlockH)
                                snake[i].h++;
                        }

                        if (snake[i].w >= fieldWidth)
                            snake[i].w = 0;
                        if (snake[i].h >= fieldHeight)
                            snake[i].h = 0;

                        if (snake[i].w < 0)
                            snake[i].w = fieldWidth - 1;
                        if (snake[i].h < 0)
                            snake[i].h = fieldHeight - 1;

                        break;
                    case fieldSnakeBodyMark:
                        if (i == 1)
                        {
                            secondBlockW = snake[i].w;
                            secondBlockH = snake[i].h;
                        }

                        if (snake[i].w != snake[i - 1].w || snake[i].h != snake[i - 1].h)
                        {
                            snake[i].w = snake[i - 1].w;
                            snake[i].h = snake[i - 1].h;
                        }

                        break;
                }
            }
        }

        private void FillField()
        {
            for (var w = 0; w < field.GetLength(0); w++)
            {
                for (var h = 0; h < field.GetLength(1); h++)
                {
                    field[w, h] = fieldEmptyMark;
                }
            }

            for (var i = 0; i < snake.Count; i++)
            {
                field[snake[i].w, snake[i].h] = snake[i].mark;
            }

            field[food.w, food.h] = food.mark;
        }

        private void FillScreenInfo()
        {
            for (var w = fieldBorder + 1; w < screenWidth; w++)
            {
                for (var h = 0; h < screenHeight; h++)
                {
                    screen[w, h] = ' ';
                }
            }

            var scoreStr = $"Score: {score}";
            var scoreMaxStr = $"Record: {scoreMax}";
            var controlsStr = $"Controls: Arrows ║ ↑ ║ ↓ ║ ← ║ → ║";
            var controlsStr2 = $"Controls: ESC to exit";

            for (var i = 0; i < screenHeight; i++)
            {
                screen[fieldBorder, i] = '▒';
                screen[fieldBorder + 1, i] = '▒';
            }

            for (var j = 0; j < scoreStr.Length; j++)
            {
                screen[fieldBorder + 3 + j, 13] = scoreStr[j];
            }

            for (var j = 0; j < scoreMaxStr.Length; j++)
            {
                screen[fieldBorder + 3 + j, 14] = scoreMaxStr[j];
            }

            for (var j = 0; j < controlsStr.Length; j++)
            {
                screen[fieldBorder + 3 + j, 15] = controlsStr[j];
            }

            for (var j = 0; j < controlsStr2.Length; j++)
            {
                screen[fieldBorder + 3 + j, 16] = controlsStr2[j];
            }
        }

        private void FillScreenField()
        {
            for (var w = 0; w < fieldWidth; w++)
            {
                for (var h = 0; h < fieldHeight; h++)
                {
                    var sw = w * 2;
                    var sw2 = w == 0 ? 1 : w * 2 + 1;

                    char sym = '\0';

                    switch (field[w, h])
                    {
                        case fieldFoodMark:
                            sym = foodView;
                            break;
                        case fieldSnakeBodyMark:
                            sym = snakeBodyView;
                            break;
                        case fieldSnakeHeadMark:
                            sym = snakeHeadView;
                            break;
                        case fieldEmptyMark:
                            sym = '\0';
                            break;
                    }

                    screen[sw, h] = sym;
                    screen[sw2, h] = sym;
                }
            }
        }

        private void DrawScreen()
        {

            for (var i = 0; i < screenHeight; i++)
            {
                Console.SetCursorPosition(0, i);

                for (var j = 0; j < screenWidth; j++)
                {
                    if (j > screenWidth - 2)
                        continue;

                    Console.Write(screen[j, i]);
                }
            }

            Console.SetCursorPosition(0, 0);
        }
    }
}
