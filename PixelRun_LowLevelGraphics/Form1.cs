using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Media;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.IO;

namespace PixelRun_LowLevelGraphics
{
    public partial class Form1 : Form
    {
        private int backgroundX = 0;
        private int backgroundSpeed = 3; 
        private Image[] backgroundImages;
        private Image runningSprite;
        private Image jumpingSprite; 
        private Image walkingSprite;
        private int jumpFrameCount = 8;
        private int runFrameCount = 6;  
        private int walkFrameCount = 6;
        private int spriteFrameWidth = 80;
        private int spriteFrameHeight = 80; 
        private int currentFrame = 0; 
        private int playerX = 50;
        private int playerY = 330; 
        private bool isJumping = false;
        private int jumpHeight = 120; 
        private int jumpSpeed = 5; 
        private bool isRunning = false; 
        private int runSpeed = 10; 
        private bool isWalking = false; 
        private bool isBackgroundMoving = false; 
        private int overlapWidth = 3; 
        private int currentImageIndex = 0; 
        private int originalPlayerY; 
        private bool isPlaying = false;
        private bool hasInitialRun = false;
        private List<Rock> rocks = new List<Rock>();
        private Image rockPic;
        private Image rockImage;
        private Image deathSprite;
        private Random random = new Random();
        private bool isDead = false;
        private int deathFrameCount = 8;
        private bool gameOver = false;
        private int framesSinceLastRock = 0;
        private const int rockSideOffset = 20; 
        private const int framesBetweenRocks = 800;  
        private System.Windows.Forms.Timer deathDelayTimer;
        private SoundPlayer backgroundPlayer = new SoundPlayer("bgMusic.wav");
        private SoundPlayer deathEffectPlayer = new SoundPlayer("death.wav");
        private int score = 0;
        private Font customFont;
        private Font introFont;
        private PrivateFontCollection privateFonts = new PrivateFontCollection();

        public Form1()
        {
            InitializeComponent();

            privateFonts.AddFontFile("C:\\Users\\Le Bronn\\source\\repos\\PixelRun_LowLevelGraphics\\PixelRun_LowLevelGraphics\\Minecraft.ttf");
            FontFamily customFontFamily = privateFonts.Families[0];
            customFont = new Font(customFontFamily, 36); 
            introFont = new Font(customFontFamily, 40, FontStyle.Bold);

            InitializeGame();
            DoubleBuffered = true;
        }

        private void InitializeGame()
        {
            backgroundPlayer.PlayLooping();

            backgroundImages = new Image[]
            {
                Properties.Resources.PixelTown_Day,
                Properties.Resources.PixelTown_Sunset,
                Properties.Resources.PixelTown_Night,
                Properties.Resources.PixelTown_WinterDay,
                Properties.Resources.PixelTown_WinterNight
            };

            runningSprite = Properties.Resources.Dude_Monster_Run_6;
            jumpingSprite = Properties.Resources.Dude_Monster_Jump_8;
            walkingSprite = Properties.Resources.Dude_Monster_Walk_6;

            for (int i = 0; i < backgroundImages.Length; i++)
            {
                backgroundImages[i] = ScaleImage(backgroundImages[i], ClientSize.Width * 2, ClientSize.Height);
            }

            rockPic = Properties.Resources.Rock2;
            rockImage = ScaleImage(rockPic, rockPic.Width * 2, rockPic.Height * 2);
            deathSprite = Properties.Resources.Dude_Monster_Death_8; 

            runningSprite = ScaleImage(runningSprite, spriteFrameWidth * runFrameCount, spriteFrameHeight);
            jumpingSprite = ScaleImage(jumpingSprite, spriteFrameWidth * jumpFrameCount, spriteFrameHeight);
            walkingSprite = ScaleImage(walkingSprite, spriteFrameWidth * walkFrameCount, spriteFrameHeight);

            System.Windows.Forms.Timer gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 16; // 60 frames per second
            gameTimer.Tick += UpdateGame;
            gameTimer.Start();

            System.Windows.Forms.Timer backgroundImageTimer = new System.Windows.Forms.Timer();
            backgroundImageTimer.Interval = 10000; // 10 seconds interval
            backgroundImageTimer.Tick += ChangeBackgroundImage;
            backgroundImageTimer.Start();

            deathDelayTimer = new System.Windows.Forms.Timer();
            deathDelayTimer.Interval = 2000;  // 2 seconds delay
            deathDelayTimer.Tick += (s, e) =>
            {
                backgroundPlayer.Stop();
                deathEffectPlayer.Play();
                gameOver = true;
                deathDelayTimer.Stop();
                MessageBox.Show("Game over, try again. Press R after to restart!", "Game Over", MessageBoxButtons.OK);
            };

            originalPlayerY = playerY;
        }

        private class Rock
        {
            public int X { get; set; }
            public int Y { get; set; }
            public Image Image { get; set; }

            public Rock(int x, int y, Image image)
            {
                X = x;
                Y = y;
                Image = image;
            }
        }

        private enum GameState
        {
            Starting,
            Playing,
            GameOver
        }

        private GameState currentState = GameState.Starting;


        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (currentState == GameState.Starting && e.KeyCode == Keys.D)
            {
                currentState = GameState.Playing;
                isPlaying = true;
                isBackgroundMoving = true;
                return;
            }

            if (gameOver && (e.KeyCode == Keys.R || e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
            {
                RestartGame();
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Right:
                case Keys.D:
                    if (!isPlaying)
                    {
                        isPlaying = true;
                        isBackgroundMoving = true;
                        isRunning = true;
                        playerX += runSpeed;
                    }
                    else
                    {
                        isRunning = true;
                    }

                    break;

                case Keys.Up:
                case Keys.W:
                    if (!isJumping)
                    {
                        isJumping = true;
                        score++;
                    }
                    break;

                case Keys.Left:
                case Keys.A:
                    if (!isJumping)
                    {
                        isWalking = true;
                    }
                    break;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            switch (e.KeyCode)
            {
                case Keys.Right:
                case Keys.D:
                    break;

                case Keys.Left:
                case Keys.A:
                    isWalking = false;
                    break;
            }
        }

        private void UpdateGame(object sender, EventArgs e)
        {
            if (gameOver) return;

            if (isDead)
            {
                if (currentFrame < deathFrameCount - 1)
                {
                    currentFrame++;
                }
                else
                {
                    currentFrame = deathFrameCount;
                }

                Invalidate();
                return;
            }

            if (isBackgroundMoving)
            {
                backgroundX -= backgroundSpeed;
                if (backgroundX <= -ClientSize.Width)
                {
                    backgroundX = 0;
                }
            }

            if (isJumping)
            {
                playerY -= jumpSpeed;
                if (playerY <= originalPlayerY - jumpHeight)
                {
                    jumpSpeed = -jumpSpeed;
                }
                if (playerY >= originalPlayerY)
                {
                    playerY = originalPlayerY;
                    jumpSpeed = Math.Abs(jumpSpeed); 
                    isJumping = false; 
                }
                currentFrame = (currentFrame + 1) % jumpFrameCount;
            }
            else if (isRunning)
            {
                currentFrame = (currentFrame + 1) % runFrameCount;
            }
            else if (isWalking)
            {
                currentFrame = (currentFrame + 1) % walkFrameCount;
            }

            if (!isDead)
            {
                if (random.Next(100) < 1) // 1% chance per frame to add a rock
                {
                    int rockY = playerY + spriteFrameHeight - rockImage.Height;
                    rocks.Add(new Rock(ClientSize.Width, rockY, rockImage));
                }
            }
            else
            {
                if (currentFrame < deathFrameCount - 1)
                {
                    currentFrame++;
                }
            }

            for (int i = rocks.Count - 1; i >= 0; i--)
            {
                if (isBackgroundMoving)
                {
                    rocks[i].X -= backgroundSpeed;
                }

                if (rocks[i].X + rockImage.Width < 0)
                {
                    rocks.RemoveAt(i);
                }
                else if (CheckCollisionWithRock(rocks[i]))
                {
                    isDead = true;
                    deathDelayTimer.Start();
                    currentFrame = 0;
                    break;
                }
            }

            framesSinceLastRock++;
            if (!isDead && framesSinceLastRock > framesBetweenRocks && new Random().Next(100) < 2)
            {
                framesSinceLastRock = 0;

                int rockY = playerY + spriteFrameHeight - rockImage.Height;
                rocks.Add(new Rock(ClientSize.Width, rockY, rockImage));
            }

            Invalidate();
        }

        private bool CheckCollisionWithRock(Rock rock)
        {
            Rectangle characterRect = new Rectangle(playerX, playerY, spriteFrameWidth, spriteFrameHeight);
            Rectangle rockHitbox = new Rectangle(rock.X + rockSideOffset, rock.Y, rockImage.Width - 2 * rockSideOffset, rockImage.Height / 2);
            return characterRect.IntersectsWith(rockHitbox);
        }

        private void ChangeBackgroundImage(object sender, EventArgs e)
        {
            currentImageIndex = (currentImageIndex + 1) % backgroundImages.Length;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            e.Graphics.DrawImage(backgroundImages[currentImageIndex], backgroundX, 0, ClientSize.Width, ClientSize.Height);
            e.Graphics.DrawImage(backgroundImages[currentImageIndex], backgroundX + ClientSize.Width - overlapWidth, 0, ClientSize.Width, ClientSize.Height);

            if (currentState == GameState.Starting)
            {
                string message = "Run, Dude Run! Press D to start!";
                SizeF messageSize = e.Graphics.MeasureString(message, introFont);
                PointF messagePosition = new PointF(65, (ClientSize.Height - messageSize.Height) / 2 + 10);

                GraphicsPath path = new GraphicsPath();
                path.AddString(message, introFont.FontFamily, (int)introFont.Style, introFont.Size, messagePosition, StringFormat.GenericDefault);
                Pen outlinePen = new Pen(Color.Black, 3);
                e.Graphics.DrawPath(outlinePen, path);
                e.Graphics.FillPath(Brushes.White, path);

                outlinePen.Dispose();
                path.Dispose();
                return;
            }

            foreach (var rock in rocks)
            {
                e.Graphics.DrawImage(rock.Image, rock.X, rock.Y);
            }

            Image currentSprite = GetCurrentSprite();
            Rectangle sourceRect = new Rectangle(currentFrame * spriteFrameWidth, 0, spriteFrameWidth, spriteFrameHeight);
            e.Graphics.DrawImage(currentSprite, new Rectangle(playerX, playerY, spriteFrameWidth, spriteFrameHeight), sourceRect, GraphicsUnit.Pixel);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddString($"Score: {score}", customFont.FontFamily, (int)customFont.Style, customFont.Size, new PointF(14, 14), StringFormat.GenericDefault);

                using (Pen outlinePen = new Pen(Color.Black, 2))  
                {
                    e.Graphics.DrawPath(outlinePen, path);
                }

                e.Graphics.FillPath(Brushes.White, path);
            }

            if (isDead)
            {
                Rectangle deathSourceRect = new Rectangle(currentFrame * (deathSprite.Width / deathFrameCount), 0, deathSprite.Width / deathFrameCount, deathSprite.Height);
                e.Graphics.DrawImage(deathSprite, new Rectangle(playerX, playerY, spriteFrameWidth, spriteFrameHeight), deathSourceRect, GraphicsUnit.Pixel);
            }
        }

        private Image GetCurrentSprite()
        {
            if (isJumping) return jumpingSprite;
            if (isRunning) return runningSprite;
            if (isWalking) return walkingSprite;
            return walkingSprite;
        }

        private void RestartGame()
        {
            score = 0;
            backgroundX = 0;
            currentFrame = 0;
            playerX = 50;
            playerY = 330;
            isJumping = false;
            isRunning = false;
            isWalking = false;
            isBackgroundMoving = false;
            currentImageIndex = 0;
            isPlaying = false;
            hasInitialRun = false;
            rocks.Clear();
            isDead = false;
            gameOver = false;
            deathEffectPlayer.Stop();
            backgroundPlayer.PlayLooping();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Handle initialization tasks when the form loads
        }

        private Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            double ratioX = (double)maxWidth / image.Width;
            double ratioY = (double)maxHeight / image.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(image.Width * ratio);
            int newHeight = (int)(image.Height * ratio);

            Bitmap newImage = new Bitmap(newWidth, newHeight);
            using (Graphics graphics = Graphics.FromImage(newImage))
            {
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return newImage;
        }
    }
}