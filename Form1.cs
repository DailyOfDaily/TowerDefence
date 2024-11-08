using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;  // Windows Media Player 라이브러리

namespace 타워디펜스
{

    public partial class TWD : Form
    {
        //비용 및 적군 수량 조절 변수
        private int point = 1200;    //시작 비용
        private int resetPoint = 2000;  //재시작 비용
        private int upgradeCost = 50;   //강화 비용
        private int uprange = 20;       //강화 범위
        private int upAttackSpeed = 25; //강화 속도
        private double upAttackPower = 1.05; //강화 공격력
        private int upgradeIncrease = 25;   //강화비 증가량
        private const int enemyCount = 99999; //적 생성 수
        private int outenemy = 50;  //아웃카운트
        private const int MaxProjectilesPerTower = 2; // 최대 투사체 수
        private int gunSpeed = 12;      //투사체 속도

        //맵과 타워 생성 및 철거 버튼
        private Panel mapPanel;
        private PictureBox[,] mapTiles = new PictureBox[10, 10];
        private List<IEnemy> enemies = new List<IEnemy>();
        private Timer spawnTimer;
        private List<PictureBox> enemyPath = new List<PictureBox>();
        private int currentSpawnIndex = 0;
        private ITower selectedTower = null; // 선택된 타워 변수
        private Button demolishButton; // 철거 버튼
        private PictureBox selectedTile = null; // 선택된 타일
        private ITower clickedTower = null; // 클릭된 타워
        private List<ITower> towers = new List<ITower>(); // 타워를 저장할 새로운 컬렉션 추가
        private PictureBox lastSelectedTile = null; // 마지막으로 선택된 타일을 저장

        //적 생성
        private int totalEnemiesSpawned = 0;
        private int remainingEnemies = 0;
        private List<IEnemy> enemyList; // 적 리스트

        //투사체
        private List<Projectile> projectiles;

        //포인트, 적, 타워 정보표시 레이블
        private Label pointLabel;
        private Label enemyInfoLabel;
        private Label towerInfoLabel;

        // 타워와 공격 타이머를 연결하는 딕셔너리
        private Dictionary<ITower, Timer> towerAttackTimers = new Dictionary<ITower, Timer>();
        private Label selectedTowerLabel;
        private Label selectedTowerLabel2;

        private bool gameStarted = false; // 게임 시작 여부를 나타내는 변수
        private Bitmap initialMapSnapshot;

        //점수 기록
        private int totalScore = 0;
        private int record_score = 0;

        //적 업그레이드 타이머
        private Timer enemyAttributeIncreaseTimer;
        private Timer enemyHealthIncreaseTimer;
        private Timer enemySpeedIncreaseTimer;

        // 타워별로 현재 발사된 투사체 개수를 저장할 딕셔너리
        private Dictionary<ITower, int> towerProjectileCount = new Dictionary<ITower, int>();
        private Label dummyLabel; // 폼의 숨겨진 Label을 이용한 포커스 제거

        //업그레이드 레이블
        private Label upgradeCostLabelA;
        private Label upgradeCostLabelB;
        private Label upgradeCostLabelC;

        //사운드
        private System.Media.SoundPlayer sound1;
        private System.Media.SoundPlayer sound2;
        private System.Media.SoundPlayer sound3;
        private System.Media.SoundPlayer sound4;
        private System.Media.SoundPlayer sound5;
        private System.Media.SoundPlayer sound6;

        public TWD()
        {
            InitializeComponent();
            InitializeMap();
            SetEnemyPath();
            InitializeSpawnTimer();
            InitializeTowerButtons(); // 타워 버튼 초기화 호출
            InitializeDemolishButton(); // 철거 버튼 초기화 호출
            InitializeTowerAttackTimers();

            projectiles = new List<Projectile>();
            enemies = new List<IEnemy>();

            this.Load += Form1_Load;
            this.KeyPreview = true; // 폼에서 키보드 입력을 받을 수 있도록 설정
            this.KeyDown += Form1_KeyDown; // 키보드 이벤트 핸들러 추가

            CaptureInitialMap(); // 초기 맵 상태를 캡처하여 저장

        }
        //초기 맵 상태 저장 >> 재시작 시 맵 클린을 위해서
        private void CaptureInitialMap()
        {
            initialMapSnapshot = new Bitmap(mapPanel.Width, mapPanel.Height);
            mapPanel.DrawToBitmap(initialMapSnapshot, new Rectangle(0, 0, mapPanel.Width, mapPanel.Height));
        }
        //키 다운 이벤트
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!gameStarted && e.KeyCode == Keys.F1)
            {
                gameStarted = true;
                spawnTimer.Start();
                sound1.Stop();
                Random random = new Random();
                int soundChoice = random.Next(1, 4);    // 랜덤 노래 재생

                if (soundChoice == 1)
                {
                    sound2.PlayLooping();
                }
                else if (soundChoice == 2)
                {
                    sound3.PlayLooping();
                }
                else
                {
                    sound4.PlayLooping();
                }

                MessageBox.Show("게임이 시작되었습니다!");
            }

            if (gameStarted)
            {
                switch (e.KeyCode)
                {
                    case Keys.Q:
                        selectedTower = new TowerA();
                        selectedTowerLabel.Text = "선택된 타워: 타워 A";
                        break;

                    case Keys.W:
                        selectedTower = new TowerB();
                        selectedTowerLabel.Text = "선택된 타워: 타워 B";
                        break;

                    case Keys.E:
                        selectedTower = new TowerC();
                        selectedTowerLabel.Text = "선택된 타워: 타워 C";
                        break;

                    case Keys.Space:
                        BuildTower(null, EventArgs.Empty);
                        break;

                    case Keys.F4:
                        DemolishTower(null, EventArgs.Empty);
                        break;

                    case Keys.Escape:
                        ResetGame();
                        break;
                }
            }
        }
        //F1 >> 재시작
        private void ResetGame()
        {
            gameStarted = false;      // 게임이 시작되지 않은 상태로 설정
            remainingEnemies = 0;     // 남은 적의 수 초기화
            totalEnemiesSpawned = 0;  // 생성된 적의 수 초기화
            point = resetPoint;              // 포인트 초기화
            currentSpawnIndex = 0;    // 스폰 인덱스 초기화
            totalScore = 0;
            upgradeCost = 55;

            sound2.Stop();
            sound3.Stop();
            sound4.Stop();
            sound1.PlayLooping();

            //레이블 업데이트
            UpdateScoreLabel();
            if (upgradeCostLabelA != null) upgradeCostLabelA.Text = $"업그레이드 비용: {upgradeCost}";
            if (upgradeCostLabelB != null) upgradeCostLabelB.Text = $"업그레이드 비용: {upgradeCost}";
            if (upgradeCostLabelC != null) upgradeCostLabelC.Text = $"업그레이드 비용: {upgradeCost}";

            // 모든 타이머 멈추기 및 초기화
            spawnTimer.Stop();
            spawnTimer.Interval = 2000; // Interval을 2000으로 초기화
            foreach (var timer in towerAttackTimers.Values)
            {
                timer.Stop();
            }
            towerAttackTimers.Clear();

            // 모든 적 제거
            foreach (var enemy in enemies.ToList())
            {
                mapPanel.Controls.Remove(enemy.Sprite); // 맵에서 적 스프라이트 제거
                enemy.Sprite.Dispose();                 // 스프라이트 자원 해제
            }
            enemies.Clear(); // 적 리스트 초기화

            // 모든 타워 제거
            foreach (var tower in towers.ToList())
            {
                if (mapPanel.Controls.Contains(tower.Sprite))
                {
                    mapPanel.Controls.Remove(tower.Sprite); // mapPanel에서 타워 스프라이트 제거
                }
                tower.Sprite.Dispose(); // 타워 스프라이트 자원 해제
            }
            towers.Clear(); // 타워 리스트 초기화

            // 각 타워의 속성 초기화
            TowerA.BaseRange = TowerA.DefaultRange;
            TowerA.BaseAttackSpeed = TowerA.DefaultAttackSpeed;
            TowerA.BaseAttackPower = TowerA.DefaultAttackPower;

            TowerB.BaseRange = TowerB.DefaultRange;
            TowerB.BaseAttackSpeed = TowerB.DefaultAttackSpeed;
            TowerB.BaseAttackPower = TowerB.DefaultAttackPower;

            TowerC.BaseRange = TowerC.DefaultRange;
            TowerC.BaseAttackSpeed = TowerC.DefaultAttackSpeed;
            TowerC.BaseAttackPower = TowerC.DefaultAttackPower;

            UpdateTowerInfo(); // 레이블에 초기화된 타워 정보를 반영

            // 초기 상태 스냅샷을 맵 패널에 적용
            if (initialMapSnapshot != null)
            {
                mapPanel.BackgroundImage = new Bitmap(initialMapSnapshot);
                mapPanel.BackgroundImageLayout = ImageLayout.None;
            }

            MessageBox.Show("게임이 실패했습니다! 초기 상태로 돌아갑니다.\n 'F1'을 눌러서 게임을 다시 시작하세요.");
        }
        //적군 만들기 : 인터페이스 활용
        public interface IEnemy
        {
            int Health { get; set; }
            int Speed { get; set; }
            int RewardPoints { get; }
            int RewardScore { get; }
            PictureBox Sprite { get; }
        }
        public class EnemyA : IEnemy
        {
            public int Health { get; set; } = 350;
            public int Speed { get; set; } = 2;
            public PictureBox Sprite { get; private set; }
            public int RewardPoints { get; private set; } = 20;
            public int RewardScore { get; private set; } = 10;

            public EnemyA()
            {
                Sprite = new PictureBox
                {
                    Size = new Size(35, 35),
                    Image = Properties.Resources.RenemyA,
                    BackColor = Color.LightBlue,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };
            }
        }
        public class EnemyB : IEnemy
        {
            public int Health { get; set; } = 250;
            public int Speed { get; set; } = 8;
            public PictureBox Sprite { get; private set; }
            public int RewardPoints { get; private set; } = 50;
            public int RewardScore { get; private set; } = 30;

            public EnemyB()
            {
                Sprite = new PictureBox
                {
                    Size = new Size(25, 25),
                    Image = Properties.Resources.RenemyB,
                    BackColor = Color.LightBlue,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };
            }
        }
        public class EnemyC : IEnemy
        {
            public int Health { get; set; } = 800;
            public int Speed { get; set; } = 1;
            public PictureBox Sprite { get; private set; }
            public int RewardPoints { get; private set; } = 95;
            public int RewardScore { get; private set; } = 50;

            public EnemyC()
            {
                Sprite = new PictureBox
                {
                    Size = new Size(45, 45),
                    Image = Properties.Resources.RenemyC,
                    BackColor = Color.LightBlue,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };
            }
        }
        //타워 만들기 : 인터페이스 활용
        public interface ITower
        {
            int Range { get; set; }
            int AttackSpeed { get; set; }
            int AttackPower { get; set; }
            int Cost { get; }
            PictureBox Sprite { get; }
        }
        public class TowerA : ITower
        {
            // 기본 값 설정
            public static readonly int DefaultRange = 150;
            public static readonly int DefaultAttackSpeed = 600;
            public static readonly int DefaultAttackPower = 55;

            // 현재 값 (업그레이드로 변경될 수 있음)
            public static int BaseRange { get; set; } = DefaultRange;
            public static int BaseAttackSpeed { get; set; } = DefaultAttackSpeed;
            public static int BaseAttackPower { get; set; } = DefaultAttackPower;
            public int Range { get; set; } = BaseRange;
            public int AttackSpeed { get; set; } = BaseAttackSpeed;
            public int AttackPower { get; set; } = BaseAttackPower;
            public int Cost { get; } = 150;
            public PictureBox Sprite { get; private set; }

            public TowerA()
            {
                Sprite = new PictureBox
                {
                    Size = new Size(40, 40),
                    Image = Properties.Resources.RtowerA,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };
            }
        }
        public class TowerB : ITower
        {
            public static readonly int DefaultRange = 100;
            public static readonly int DefaultAttackSpeed = 200;
            public static readonly int DefaultAttackPower = 15;

            public static int BaseRange { get; set; } = DefaultRange;
            public static int BaseAttackSpeed { get; set; } = DefaultAttackSpeed;
            public static int BaseAttackPower { get; set; } = DefaultAttackPower;
            public int Range { get; set; } = BaseRange;
            public int AttackSpeed { get; set; } = BaseAttackSpeed;
            public int AttackPower { get; set; } = BaseAttackPower;
            public int Cost { get; } = 200;
            public PictureBox Sprite { get; private set; }

            public TowerB()
            {
                Sprite = new PictureBox
                {
                    Size = new Size(40, 40),
                    Image = Properties.Resources.RtowerB,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };
            }
        }
        public class TowerC : ITower
        {
            public static readonly int DefaultRange = 180;
            public static readonly int DefaultAttackSpeed = 1000;
            public static readonly int DefaultAttackPower = 90;

            public static int BaseRange { get; set; } = DefaultRange;
            public static int BaseAttackSpeed { get; set; } = DefaultAttackSpeed;
            public static int BaseAttackPower { get; set; } = DefaultAttackPower;
            public int Range { get; set; } = BaseRange;
            public int AttackSpeed { get; set; } = BaseAttackSpeed;
            public int AttackPower { get; set; } = BaseAttackPower;
            public int Cost { get; } = 250;
            public PictureBox Sprite { get; private set; }

            public TowerC()
            {
                Sprite = new PictureBox
                {
                    Size = new Size(40, 40),
                    Image = Properties.Resources.RtowerC,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Size = new Size(1200, 800);

            // 적 정보 표시 레이블
            enemyInfoLabel = new Label
            {
                Location = new Point(620, 150),
                Size = new Size(300, 250),
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.TopLeft
            };
            Controls.Add(enemyInfoLabel);

            // 타워 정보 표시 레이블
            towerInfoLabel = new Label
            {
                Location = new Point(620, 410),
                Size = new Size(300, 250),
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.TopLeft
            };
            Controls.Add(towerInfoLabel);

            // RichTextBox 설정
            var selectedTowerInfoBox = new RichTextBox
            {
                Location = new Point(mapPanel.Location.X, mapPanel.Location.Y + mapPanel.Height + 10),
                Size = new Size(500, 170),
                Font = new Font("맑은고딕", 9, FontStyle.Bold),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = this.BackColor, // 폼과 같은 배경색으로 설정하여 Label처럼 보이게 함
            };

            // 줄 간격 조정
            selectedTowerInfoBox.Text = "\n'F1' 입력시 게임 시작  /  'ESC' 입력시 게임 종료\n" +
                "마우스로 타워를 생성할 영역을 선택하면 노란색으로 변합니다. 생성하고 싶은 타워를\n" +
                "선택하고, 스페이스바를 누르거나 생성버튼을 클릭하면 타워가 생성됩니다.\n" +
                "타워 철거는 타워 선택 후 빨갛게 변하면 철거버튼이 활성화되어 철거가능 합니다.\n" +
                "시간이 지날수록 나오는 적의 출현이 빨라지고, 체력과 이동속도가 증가합니다.\n" +
                $"남은 적의 수가 '{outenemy}' (이)가 넘어가면 실패입니다.";

            // 줄 간격 설정
            selectedTowerInfoBox.SelectionStart = 0;
            selectedTowerInfoBox.SelectionLength = selectedTowerInfoBox.Text.Length;
            selectedTowerInfoBox.SelectionCharOffset = 4; // 값 조절로 줄 간격 변경
            selectedTowerInfoBox.DeselectAll();

            Controls.Add(selectedTowerInfoBox);

            selectedTowerLabel = new Label
            {
                Location = new Point(mapPanel.Location.X, mapPanel.Location.Y + mapPanel.Height + 200), // mapPanel 아래쪽에 위치
                Size = new Size(300, 100),
                AutoSize = false,
                Font = new Font("맑은고딕", 10, FontStyle.Bold),
                Text = "선택된 타워: 없음", // 초기 텍스트 설정
                TextAlign = ContentAlignment.TopLeft
            };
            Controls.Add(selectedTowerLabel);

            // 적과 타워 정보 UI 업데이트 호출
            UpdateEnemyInfo();
            UpdateTowerInfo();
            UpdateScoreLabel();
            InitializeUpgradeUI();

            //wav파일 재생을 위한 설정
            sound1 = new System.Media.SoundPlayer(Properties.Resources.loading);
            sound2 = new System.Media.SoundPlayer(Properties.Resources.start1);
            sound3 = new System.Media.SoundPlayer(Properties.Resources.start2);
            sound4 = new System.Media.SoundPlayer(Properties.Resources.enemy);
            sound5 = new System.Media.SoundPlayer(Properties.Resources.Build);
            sound6 = new System.Media.SoundPlayer(Properties.Resources.BOOM);
            sound1.PlayLooping(); // 무한 반복 실행

        }

        // 적 정보 업데이트
        public void UpdateEnemyInfo()
        {
            var enemyA = new EnemyA();
            var enemyB = new EnemyB();
            var enemyC = new EnemyC();

            // 적 정보 문자열 생성
            string enemyInfo = "\n적군 정보\n\n\n" +
                               $"EnemyA (유령)\n\n   체력 : {new EnemyA().Health}, 이동속도 : {new EnemyA().Speed}\n\n\n" +
                               $"EnemyB (초록)\n\n   체력 : {new EnemyB().Health}, 이동속도 : {new EnemyB().Speed}\n\n\n" +
                               $"EnemyC (파랑)\n\n   체력 : {new EnemyC().Health}, 이동속도 : {new EnemyC().Speed}";

            // 레이블에 정보 설정
            enemyInfoLabel.Text = enemyInfo;
        }
        // 타워 정보 업데이트
        public void UpdateTowerInfo()
        {
            // 각 적 클래스 인스턴스 생성
            var towerA = new TowerA();
            var towerB = new TowerB();
            var towerC = new TowerC();

            // 타워 정보 문자열 생성
            string towerInfo = "\n타워 정보\n\n\n" +
                                $"TowerA - 비용: {towerA.Cost}\n\n   공격범위: {TowerA.BaseRange}, 공격속도: {TowerA.BaseAttackSpeed}, 공격력: {TowerA.BaseAttackPower}\n\n\n" +
                                $"TowerB - 비용: {towerB.Cost}\n\n   공격범위: {TowerB.BaseRange}, 공격속도: {TowerB.BaseAttackSpeed}, 공격력: {TowerB.BaseAttackPower}\n\n\n" +
                                $"TowerC - 비용: {towerC.Cost}\n\n   공격범위: {TowerC.BaseRange}, 공격속도: {TowerC.BaseAttackSpeed}, 공격력: {TowerC.BaseAttackPower}";

            // 레이블에 결과 텍스트 설정 : 업그레이드로 인한 증가값 실시간 반영
            towerInfoLabel.Text = towerInfo;
        }

        // 타워 업그레이드 UI를 생성
        private void InitializeUpgradeUI()
        {
            // '타워 업그레이드' 레이블 생성
            Label upgradeLabel = new Label
            {
                Text = "타워 업그레이드",
                Location = new Point(950, 10),
                Size = new Size(200, 30),
                Font = new Font("Arial", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(upgradeLabel);

            // 타워 A 업그레이드 버튼 생성
            CreateTowerUpgradeSection("타워 A", new TowerA(), 50);

            // 타워 B 업그레이드 버튼 생성
            CreateTowerUpgradeSection("타워 B", new TowerB(), 220);

            // 타워 C 업그레이드 버튼 생성
            CreateTowerUpgradeSection("타워 C", new TowerC(), 390);
        }

        // 각 타워의 업그레이드 섹션을 생성
        private void CreateTowerUpgradeSection(string towerName, ITower tower, int yOffset)
        {
            // 타워 이름 레이블
            Label towerLabel = new Label
            {
                Text = towerName,
                Location = new Point(950, yOffset),
                Size = new Size(50, 30),
                Font = new Font("Arial", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(towerLabel);

            // 업그레이드 비용 레이블
            Label upgradeCostLabel = new Label
            {
                Text = $"업그레이드 비용: {upgradeCost}", // 초기 비용
                Location = new Point(1000, yOffset), // 타워 이름 라벨 옆에 배치
                Size = new Size(150, 30),
                Font = new Font("Arial", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(upgradeCostLabel);

            // 각 타워에 대해 레이블 저장
            if (tower is TowerA) upgradeCostLabelA = upgradeCostLabel;
            else if (tower is TowerB) upgradeCostLabelB = upgradeCostLabel;
            else if (tower is TowerC) upgradeCostLabelC = upgradeCostLabel;

            // 공격범위 업그레이드 버튼
            Button rangeUpgradeButton = new Button
            {
                Text = $"공격범위 +{uprange}",
                Location = new Point(950, yOffset + 40),
                Size = new Size(200, 30)
            };
            rangeUpgradeButton.Click += (sender, e) => UpgradeRange(tower);
            Controls.Add(rangeUpgradeButton);

            // 공격속도 업그레이드 버튼
            Button speedUpgradeButton = new Button
            {
                Text = $"공격속도 +{upAttackSpeed}",
                Location = new Point(950, yOffset + 80),
                Size = new Size(200, 30)
            };
            speedUpgradeButton.Click += (sender, e) => UpgradeAttackSpeed(tower);
            Controls.Add(speedUpgradeButton);

            // 공격력 업그레이드 버튼
            Button powerUpgradeButton = new Button
            {
                Text = $"공격력 +{(upAttackPower - 1) * 100}%",
                Location = new Point(950, yOffset + 120),
                Size = new Size(200, 30)
            };
            powerUpgradeButton.Click += (sender, e) => UpgradeAttackPower(tower);
            Controls.Add(powerUpgradeButton);
        }
        // 공격범위 업그레이드
        private void UpgradeRange(ITower tower)
        {
            if (point >= upgradeCost) // 포인트가 충분한지 확인
            {
                point -= upgradeCost;
                pointLabel.Text = $"포인트: {point}"; // 포인트 레이블 업데이트

                // 업그레이드 후 비용 증가
                upgradeCost += upgradeIncrease;

                upgradeCostLabelA.Text = $"업그레이드 비용: {upgradeCost}";
                upgradeCostLabelB.Text = $"업그레이드 비용: {upgradeCost}";
                upgradeCostLabelC.Text = $"업그레이드 비용: {upgradeCost}";

                if (tower is TowerA)
                {
                    TowerA.BaseRange += uprange;
                    foreach (var t in towers.OfType<TowerA>())
                    {
                        t.Range = TowerA.BaseRange;
                    }
                    MessageBox.Show($"TowerA의 공격범위가 {TowerA.BaseRange}(으)로 증가했습니다!");
                }
                else if (tower is TowerB)
                {
                    TowerB.BaseRange += uprange;
                    foreach (var t in towers.OfType<TowerB>())
                    {
                        t.Range = TowerB.BaseRange;
                    }
                    MessageBox.Show($"TowerB의 공격범위가 {TowerB.BaseRange}(으)로 증가했습니다!");
                }
                else if (tower is TowerC)
                {
                    TowerC.BaseRange += uprange;
                    foreach (var t in towers.OfType<TowerC>())
                    {
                        t.Range = TowerC.BaseRange;
                    }
                    MessageBox.Show($"TowerC의 공격범위가 {TowerC.BaseRange}(으)로 증가했습니다!");
                }
                UpdateTowerInfo();

                // 폼에 포커스를 설정하여 버튼 포커스를 제거
                this.ActiveControl = null;
            }
            else
            {
                MessageBox.Show("포인트가 부족합니다!");
            }
        }
        // 공격속도 업그레이드
        private void UpgradeAttackSpeed(ITower tower)
        {
            // 공격속도가 지정 값에 도달하면(150ms인 경우) 업그레이드를 막고 메시지 출력
            if ((tower is TowerA && TowerA.BaseAttackSpeed <= 150) ||
                (tower is TowerB && TowerB.BaseAttackSpeed <= 150) ||
                (tower is TowerC && TowerC.BaseAttackSpeed <= 150))
            {
                MessageBox.Show("더 이상 업그레이드할 수 없습니다.");
                return;
            }

            if (point >= upgradeCost)
            {
                point -= upgradeCost;
                pointLabel.Text = $"포인트: {point}";

                upgradeCost += upgradeIncrease;

                upgradeCostLabelA.Text = $"업그레이드 비용: {upgradeCost}";
                upgradeCostLabelB.Text = $"업그레이드 비용: {upgradeCost}";
                upgradeCostLabelC.Text = $"업그레이드 비용: {upgradeCost}";

                if (tower is TowerA)
                {
                    TowerA.BaseAttackSpeed = Math.Max(150, TowerA.BaseAttackSpeed - upAttackSpeed);
                    foreach (var t in towers.OfType<TowerA>())
                    {
                        t.AttackSpeed = TowerA.BaseAttackSpeed;
                        ResetTowerAttackTimer(t);
                    }
                    MessageBox.Show($"TowerA의 공격속도가 {TowerA.BaseAttackSpeed}ms로 감소했습니다!");
                }
                else if (tower is TowerB)
                {
                    TowerB.BaseAttackSpeed = Math.Max(150, TowerB.BaseAttackSpeed - upAttackSpeed);
                    foreach (var t in towers.OfType<TowerB>())
                    {
                        t.AttackSpeed = TowerB.BaseAttackSpeed;
                        ResetTowerAttackTimer(t);
                    }
                    MessageBox.Show($"TowerB의 공격속도가 {TowerB.BaseAttackSpeed}ms로 감소했습니다!");
                }
                else if (tower is TowerC)
                {
                    TowerC.BaseAttackSpeed = Math.Max(150, TowerC.BaseAttackSpeed - upAttackSpeed);
                    foreach (var t in towers.OfType<TowerC>())
                    {
                        t.AttackSpeed = TowerC.BaseAttackSpeed;
                        ResetTowerAttackTimer(t);
                    }
                    MessageBox.Show($"TowerC의 공격속도가 {TowerC.BaseAttackSpeed}ms로 감소했습니다!");
                }
                UpdateTowerInfo();
            }
            else
            {
                MessageBox.Show("포인트가 부족합니다!");
            }
        }
        // 타워 공격 타이머를 재설정
        private void ResetTowerAttackTimer(ITower tower)
        {
            if (towerAttackTimers.ContainsKey(tower))
            {
                // 기존 타이머 중지 및 제거
                towerAttackTimers[tower].Stop();
                towerAttackTimers[tower].Dispose();
                towerAttackTimers.Remove(tower);
            }

            // 새로운 타이머 설정 및 시작
            Timer newAttackTimer = new Timer { Interval = tower.AttackSpeed };
            newAttackTimer.Tick += (s, e) => ShootProjectile(tower);
            newAttackTimer.Start();
            towerAttackTimers[tower] = newAttackTimer;
        }

        // 공격력 업그레이드
        private void UpgradeAttackPower(ITower tower)
        {
            if (point >= upgradeCost)
            {
                point -= upgradeCost;
                pointLabel.Text = $"포인트: {point}";

                upgradeCost += upgradeIncrease;

                upgradeCostLabelA.Text = $"업그레이드 비용: {upgradeCost}";
                upgradeCostLabelB.Text = $"업그레이드 비용: {upgradeCost}";
                upgradeCostLabelC.Text = $"업그레이드 비용: {upgradeCost}";

                if (tower is TowerA)
                {
                    TowerA.BaseAttackPower = (int)Math.Ceiling(TowerA.BaseAttackPower * upAttackPower);
                    foreach (var t in towers.OfType<TowerA>())
                    {
                        t.AttackPower = TowerA.BaseAttackPower;
                    }
                    MessageBox.Show($"TowerA의 공격력이 {TowerA.BaseAttackPower}(으)로 증가했습니다!");
                }
                else if (tower is TowerB)
                {
                    TowerB.BaseAttackPower = (int)Math.Ceiling(TowerB.BaseAttackPower * upAttackPower);
                    foreach (var t in towers.OfType<TowerB>())
                    {
                        t.AttackPower = TowerB.BaseAttackPower;
                    }
                    MessageBox.Show($"TowerB의 공격력이 {TowerB.BaseAttackPower}(으)로 증가했습니다!");
                }
                else if (tower is TowerC)
                {
                    TowerC.BaseAttackPower = (int)Math.Ceiling(TowerC.BaseAttackPower * upAttackPower);
                    foreach (var t in towers.OfType<TowerC>())
                    {
                        t.AttackPower = TowerC.BaseAttackPower;
                    }
                    MessageBox.Show($"TowerC의 공격력이 {TowerC.BaseAttackPower}(으)로 증가했습니다!");
                }
                UpdateTowerInfo();

                this.ActiveControl = null;
            }
            else
            {
                MessageBox.Show("포인트가 부족합니다!");
            }
        }
        //투사체 제거를 위한 게임 업데이트
        private void UpdateGame()
        {
            // 투사체 리스트를 반복하며 충돌 감지
            foreach (var projectile in projectiles.ToList())
            {
                projectile.CheckCollisionWithEnemy();

                // 충돌 발생 시 투사체를 UI에서 제거
                if (projectile.HasHitTarget)
                {
                    projectile.RemoveFromGame(this); // UI에서 제거
                    projectiles.Remove(projectile); // 리스트에서 제거
                }
            }
        }
        //게임 오버시
        private void GameOver()
        {
            sound2.Stop();
            sound3.Stop();
            sound4.Stop();
            sound1.PlayLooping();

            // 모든 타이머를 멈춤
            spawnTimer.Stop();
            foreach (var timer in towerAttackTimers.Values)
            {
                timer.Stop();
            }

            // 실패 메시지박스를 표시
            MessageBox.Show($"게임 실패! 남은 적 수가 {outenemy} (을)를 초과했습니다.");
        }
        // 타워 공격 타이머
        private void InitializeTowerAttackTimers()
        {
            foreach (var tower in towers)
            {
                Timer attackTimer = new Timer { Interval = tower.AttackSpeed };
                attackTimer.Tick += (sender, e) => ShootProjectile(tower);
                attackTimer.Start();
            }
        }
        // 타워 공격 타이머 초기화 메서드 수정
        private void InitializeTowerAttackTimer(ITower tower)
        {
            Timer attackTimer = new Timer { Interval = tower.AttackSpeed };
            attackTimer.Tick += (s, args) => ShootProjectile(tower);
            attackTimer.Start();

            // 타워와 타이머를 딕셔너리에 추가
            towerAttackTimers[tower] = attackTimer;
        }
        //투사체 생성, 제거 및 이동
        private void ShootProjectile(ITower tower)
        {
            // 타워가 철거되었거나 리스트에 없는 경우 즉시 반환
            if (!towers.Contains(tower)) return;

            // 현재 타워의 발사된 투사체 수를 가져오거나 0으로 초기화
            if (!towerProjectileCount.ContainsKey(tower))
            {
                towerProjectileCount[tower] = 0;
            }

            // 최대 투사체 수를 초과하는지 확인
            if (towerProjectileCount[tower] < MaxProjectilesPerTower)
            {
                // 타워에서 투사체 생성
                var targetEnemy = enemies
                    .Where(enemy => IsEnemyInRange(tower, enemy) && enemy.Health > 0)
                    .OrderBy(enemy => DistanceToTower(tower, enemy))
                    .FirstOrDefault();

                if (targetEnemy != null)
                {
                    var projectile = new Projectile(tower, targetEnemy);
                    mapPanel.Controls.Add(projectile.Sprite);
                    projectile.Sprite.BringToFront();

                    // 투사체 개수 증가
                    towerProjectileCount[tower]++;

                    // 투사체를 이동시키는 타이머 설정
                    Timer projectileTimer = new Timer { Interval = 1 };
                    projectileTimer.Tick += (sender, e) => MoveProjectile(projectile, projectileTimer);
                    projectileTimer.Start();
                }
            }
            else
            {
                //Console.WriteLine($"{tower.GetType().Name}는 최대 {MaxProjectilesPerTower}개의 투사체만 발사할 수 있습니다.");
            }
        }
        //투사체 충돌로 인한 적군 HP 감소와 제거
        private void MoveProjectile(Projectile projectile, Timer projectileTimer)
        {
            if (projectile.HasHitTarget || projectile.Target == null || projectile.Target.Health <= 0)
            {
                projectileTimer.Stop();
                mapPanel.Controls.Remove(projectile.Sprite);
                projectile.Sprite.Dispose();

                // 투사체 수 감소
                if (towerProjectileCount.ContainsKey(projectile.Tower) && towerProjectileCount[projectile.Tower] > 0)
                {
                    towerProjectileCount[projectile.Tower]--;
                }
                return;
            }

            // 나머지 이동 및 충돌 처리 로직
            if (DistanceToEnemy(projectile) < 15)
            {
                projectile.HasHitTarget = true;
                projectileTimer.Stop();
                mapPanel.Controls.Remove(projectile.Sprite);
                projectile.Sprite.Dispose();

                // 적에게 피해 적용 및 제거
                projectile.Target.Health -= projectile.Tower.AttackPower;
                if (projectile.Target.Health <= 0)
                {
                    RemoveEnemy(projectile.Target);
                }

                // 투사체 수 감소
                if (towerProjectileCount.ContainsKey(projectile.Tower) && towerProjectileCount[projectile.Tower] > 0)
                {
                    towerProjectileCount[projectile.Tower]--;
                }
            }
            else
            {
                int step = gunSpeed;
                var dx = projectile.Target.Sprite.Location.X - projectile.Sprite.Location.X;
                var dy = projectile.Target.Sprite.Location.Y - projectile.Sprite.Location.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);

                projectile.Sprite.Location = new Point(
                    projectile.Sprite.Location.X + (int)(step * dx / distance),
                    projectile.Sprite.Location.Y + (int)(step * dy / distance)
                );
            }
        }
        //투사체가 적과의 거리 찾는 함수
        private double DistanceToEnemy(Projectile projectile)
        {
            var projectileCenter = new Point(
                projectile.Sprite.Location.X + projectile.Sprite.Width / 2,
                projectile.Sprite.Location.Y + projectile.Sprite.Height / 2
            );
            var enemyCenter = new Point(
                projectile.Target.Sprite.Location.X + projectile.Target.Sprite.Width / 2,
                projectile.Target.Sprite.Location.Y + projectile.Target.Sprite.Height / 2
            );

            return Math.Sqrt(Math.Pow(projectileCenter.X - enemyCenter.X, 2) +
                             Math.Pow(projectileCenter.Y - enemyCenter.Y, 2));
        }
        //적이 타워의 범위 안에 있으면 투사체 발사하는 함수
        private void AttackEnemiesInRange(ITower tower)
        {
            var targetEnemy = enemies
                .Where(enemy => IsEnemyInRange(tower, enemy) && enemy.Health > 0)
                .OrderBy(enemy => DistanceToTower(tower, enemy))
                .FirstOrDefault();

            if (targetEnemy != null)
            {
                targetEnemy.Health -= tower.AttackPower;
                if (targetEnemy.Health <= 0)
                {
                    RemoveEnemy(targetEnemy);
                    AddScore(targetEnemy);
                }
            }
        }
        //타워와 적 사이의 거리를 계산하여 적이 타워의 사정거리 안에 있는지 확인하는 함수
        private bool IsEnemyInRange(ITower tower, IEnemy enemy)
        {
            double distance = DistanceToTower(tower, enemy);
            return distance <= tower.Range;
        }
        //타워와 적 사이의 중심점을 기준으로 거리를 계산하는 함수
        private double DistanceToTower(ITower tower, IEnemy enemy)
        {
            var towerCenter = new Point(
                tower.Sprite.Location.X + tower.Sprite.Width / 2,
                tower.Sprite.Location.Y + tower.Sprite.Height / 2
            );
            var enemyCenter = new Point(
                enemy.Sprite.Location.X + enemy.Sprite.Width / 2,
                enemy.Sprite.Location.Y + enemy.Sprite.Height / 2
            );

            return Math.Sqrt(Math.Pow(towerCenter.X - enemyCenter.X, 2) +
                             Math.Pow(towerCenter.Y - enemyCenter.Y, 2));
        }
        //투사체로 인해 적의 HP가 0 이하가 되면 적과 투사체를 제거하고 점수 반영
        private void RemoveEnemy(IEnemy enemy)
        {
            if (enemies.Contains(enemy))
            {
                // enemies 리스트와 UI에서 적 제거
                enemies.Remove(enemy);
                mapPanel.Controls.Remove(enemy.Sprite);

                // 해당 enemy를 타겟으로 하는 모든 투사체 제거
                foreach (var projectile in projectiles.ToList())
                {
                    if (projectile.Target == enemy)
                    {
                        projectile.InvalidateTarget(); // 투사체에 타겟 제거 알림
                        projectile.RemoveFromGame(this);
                        projectiles.Remove(projectile);
                    }
                }

                // 점수를 업데이트하고 남은 적 수 감소
                AddScore(enemy);
                totalScore += enemy.RewardScore;
                remainingEnemies--;
                UpdateScoreLabel(); // 라벨 업데이트 호출

                // remainingEnemies가 30을 넘으면 게임 종료
                if (remainingEnemies > outenemy)
                {
                    ResetGame();
                }
            }
        }
        //점수와 포인트 획득 업데이트
        private void AddScore(IEnemy enemy)
        {
            // 각 적의 RewardPoints 속성을 사용하여 점수를 획득
            int pointEarned = enemy.RewardPoints;

            point += pointEarned; // 포인트 증가
            UpdateScoreLabel();
            //pointLabel.Text = $"\n포인트: {point}\n\n"; // 업데이트된 포인트를 라벨에 반영

            // totalScore는 RewardScore를 기준으로 누적
            totalScore += enemy.RewardScore;

            // 신기록 조건 확인 및 갱신
            if (record_score < totalScore)
            {
                record_score = totalScore + enemy.RewardScore; // 신기록 갱신
            }
        }
        // 포인트, 스코어, 적의 수 레이블 업데이트
        private void UpdateScoreLabel()
        {
            pointLabel.Text = $"포인트(비용): {point}\n\n신기록: {record_score}\n\n누적 스코어: {totalScore}\n\n생성된 적 수: {totalEnemiesSpawned}\n\n남은 적 수: {remainingEnemies}";
        }

        // 투사체 클래스 정의
        public class Projectile
        {
            public ITower Tower { get; private set; }
            public IEnemy Target { get; private set; }
            public PictureBox Sprite { get; private set; }

            // 외부에서 값을 설정할 수 있도록 get과 set 접근자 추가
            public bool HasHitTarget { get; set; } = false;

            public Projectile(ITower tower, IEnemy target)
            {
                Tower = tower;
                Target = target;
                Sprite = new PictureBox
                {
                    Size = new Size(30, 30),
                    Image = GetProjectileImage(tower),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Visible = true
                };
                HasHitTarget = false;

                // 타워의 중앙 위치에서 생성
                Sprite.Location = new Point(
                    tower.Sprite.Location.X + tower.Sprite.Width / 2 - Sprite.Width / 2,
                    tower.Sprite.Location.Y + tower.Sprite.Height / 2 - Sprite.Height / 2
                );
                HasHitTarget = false;
            }
            // 적이 제거된 경우 호출할 메서드
            public void InvalidateTarget()
            {
                Target = null;
            }
            public bool IsTargetInvalid()
            {
                return Target == null || Target.Health <= 0;
            }
            private Image GetProjectileImage(ITower tower)
            {
                if (tower is TowerA) return Properties.Resources.RgunA;
                if (tower is TowerB) return Properties.Resources.RgunB;
                if (tower is TowerC) return Properties.Resources.RgunC;
                return Properties.Resources.RgunA; // 기본 이미지
            }
            public void CheckCollisionWithEnemy()
            {
                // 적과의 충돌 감지
                if (!HasHitTarget && Target.Sprite.Bounds.IntersectsWith(Sprite.Bounds))
                {
                    HasHitTarget = true;
                    OnHit();  // 충돌 시 호출되는 메서드
                }
            }
            private void OnHit()
            {
                // 충돌 시 투사체를 사라지게 하는 로직
                Sprite.Visible = false; // 보이지 않게 설정
                                        // 필요시 리소스 해제 등 추가 작업을 수행
            }
            public void RemoveFromGame(Control control)
            {
                // UI에서 제거
                control.Controls.Remove(Sprite);
            }
        }
        //맵 생성
        private void InitializeMap()
        {
            mapPanel = new Panel
            {
                Size = new Size(500, 500),
                Location = new Point(10, 10),
                BackColor = Color.LightGray
            };
            this.Controls.Add(mapPanel);

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    PictureBox tile = new PictureBox
                    {
                        Size = new Size(50, 50),
                        Location = new Point(col * 50, row * 50),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.White,
                        Name = "map" + (row * 10 + col + 1)
                    };

                    tile.Click += mapTile_Click; // 타일 클릭 이벤트 추가
                    mapPanel.Controls.Add(tile);
                    mapTiles[row, col] = tile;
                }
            }
        }
        //적군의 이동경로 설정
        private void SetEnemyPath()
        {
            enemyPath.Clear();

            for (int i = 1; i <= 10; i++)
                enemyPath.Add(mapPanel.Controls.Find("map" + i, true).FirstOrDefault() as PictureBox);

            for (int i = 2; i <= 10; i++)
                enemyPath.Add(mapPanel.Controls.Find("map" + (i * 10), true).FirstOrDefault() as PictureBox);

            for (int i = 1; i < 10; i++)
                enemyPath.Add(mapPanel.Controls.Find("map" + (10 * 10 - i), true).FirstOrDefault() as PictureBox);

            for (int i = 9; i >= 1; i--)
                enemyPath.Add(mapPanel.Controls.Find("map" + ((i * 10) + 1), true).FirstOrDefault() as PictureBox);

            for (int i = 1; i <= 10; i++)
                mapPanel.Controls.Find("map" + i, true).FirstOrDefault().BackColor = Color.LightBlue;
            for (int i = 2; i <= 10; i++)
                mapPanel.Controls.Find("map" + (i * 10), true).FirstOrDefault().BackColor = Color.LightBlue;
            for (int i = 1; i < 10; i++)
                mapPanel.Controls.Find("map" + (10 * 10 - i), true).FirstOrDefault().BackColor = Color.LightBlue;
            for (int i = 9; i >= 1; i--)
                mapPanel.Controls.Find("map" + ((i * 10) + 1), true).FirstOrDefault().BackColor = Color.LightBlue;
        }
        //적의 리스폰 타이머
        private void InitializeSpawnTimer()
        {
            spawnTimer = new Timer { Interval = 3000 };
            spawnTimer.Tick += (sender, e) =>
            {
                SpawnEnemy(sender, e);

                // Interval 값을 1초마다 50씩 줄이고, 최소 1000ms 이하로 내려가지 않도록 설정
                if (spawnTimer.Interval > 1000)
                {
                    spawnTimer.Interval = Math.Max(1000, spawnTimer.Interval - 50);
                }

                // Interval이 1000ms가 되면 HP및 스피드 증가 타이머들을 초기화 및 시작
                if (spawnTimer.Interval == 1000 && enemyHealthIncreaseTimer == null && enemySpeedIncreaseTimer == null)
                {
                    InitializeEnemyAttributeIncreaseTimers();
                }
            };
        }

        private void InitializeEnemyAttributeIncreaseTimers()
        {
            // 체력 증가 타이머: 10초마다 발동
            enemyHealthIncreaseTimer = new Timer { Interval = 10000 };
            enemyHealthIncreaseTimer.Tick += (sender, e) => IncreaseEnemyHealth();
            enemyHealthIncreaseTimer.Start();

            // 속도 증가 타이머: 30초마다 발동
            enemySpeedIncreaseTimer = new Timer { Interval = 30000 };
            enemySpeedIncreaseTimer.Tick += (sender, e) => IncreaseEnemySpeed();
            enemySpeedIncreaseTimer.Start();
        }

        private void IncreaseEnemyHealth()
        {
            // 모든 적의 체력을 10% 증가
            foreach (var enemy in enemies)
            {
                enemy.Health = (int)Math.Ceiling(enemy.Health * 1.1);
            }
            UpdateEnemyInfo(); // UI에 반영
        }

        private void IncreaseEnemySpeed()
        {
            // 모든 적의 속도를 1 증가
            foreach (var enemy in enemies)
            {
                enemy.Speed += 1;
            }
            UpdateEnemyInfo(); // UI에 반영
        }
        private void SpawnEnemy(object sender, EventArgs e)
        {
            if (currentSpawnIndex >= enemyCount)
            {
                spawnTimer.Stop();
                return;
            }

            // spawnTimer.Interval이 1000일 때는 빠른생성 호출, 그렇지 않으면 기본생성 호출
            IEnemy enemy = (spawnTimer.Interval == 1000) ? CreateRandomEnemyFast() : CreateRandomEnemyNormal();
            totalEnemiesSpawned++;
            remainingEnemies++;

            if (remainingEnemies > outenemy)
            {
                ResetGame();
                return;
            }

            if (enemyPath.Count > 0 && enemyPath[0] != null)
            {
                PictureBox startTile = enemyPath[0];
                enemy.Sprite.Location = new Point(
                    startTile.Location.X + (startTile.Width - enemy.Sprite.Width) / 2,
                    startTile.Location.Y + (startTile.Height - enemy.Sprite.Height) / 2
                );
                enemies.Add(enemy);
                mapPanel.Controls.Add(enemy.Sprite);
                enemy.Sprite.BringToFront();
            }

            Timer moveTimer = new Timer { Interval = 1 };
            int enemyPathIndex = 0;
            moveTimer.Tick += (s, args) => MoveEnemy(enemy, moveTimer, ref enemyPathIndex);
            moveTimer.Start();

            currentSpawnIndex++;
            UpdateScoreLabel();
        }
        // 기본 적 생성 메서드 (일반 상태에서 호출)
        private IEnemy CreateRandomEnemyNormal()
        {
            Random rand = new Random();
            int type = rand.Next(11);

            if (type == 0 || type == 1 || type == 2 || type == 3 || type == 4)
            {
                return new EnemyA();
            }
            else if (type == 5 || type == 6 || type == 7 || type == 8)
            {
                return new EnemyB();
            }
            else
            {
                return new EnemyC();
            }
        }

        // 빠른 적 생성 메서드 (spawnTimer.Interval이 1000일 때 호출)
        private IEnemy CreateRandomEnemyFast()
        {
            Random rand = new Random();
            int type = rand.Next(17); // 더 강한 적들이 더 자주 나오도록 확률 조정

            if (type == 0 || type == 1 || type == 2 || type == 3 || type == 4)
            {
                return new EnemyA();
            }
            else if (type == 5 || type == 6 || type == 7 || type == 8 || type == 9)
            {
                return new EnemyB();
            }
            else
            {
                return new EnemyC();
            }
        }
        //적군이 설정한 이동경로로 이동하도록 하는 함수
        private void MoveEnemy(IEnemy enemy, Timer moveTimer, ref int enemyPathIndex)
        {
            if (enemyPath.Count == 0) return;

            if (enemyPathIndex < enemyPath.Count && enemyPath[enemyPathIndex] != null)
            {
                PictureBox targetTile = enemyPath[enemyPathIndex];
                Point currentTarget = new Point(
                    targetTile.Location.X + (targetTile.Width - enemy.Sprite.Width) / 2,
                    targetTile.Location.Y + (targetTile.Height - enemy.Sprite.Height) / 2
                );

                if (enemy.Sprite.Location == currentTarget)
                {
                    enemyPathIndex++;
                    if (enemyPathIndex >= enemyPath.Count)
                        enemyPathIndex = 0;
                    return;
                }

                int step = enemy.Speed;
                int dx = currentTarget.X - enemy.Sprite.Location.X;
                int dy = currentTarget.Y - enemy.Sprite.Location.Y;

                int moveX = Math.Abs(dx) < step ? dx : step * Math.Sign(dx);
                int moveY = Math.Abs(dy) < step ? dy : step * Math.Sign(dy);

                enemy.Sprite.Location = new Point(enemy.Sprite.Location.X + moveX, enemy.Sprite.Location.Y + moveY);
            }
            else
            {
                moveTimer.Stop();
                mapPanel.Controls.Remove(enemy.Sprite);
            }
        }
        //타워 선택 및 생성 버튼
        private void InitializeTowerButtons()
        {
            // 타워 A 버튼
            Button towerAButton = new Button
            {
                Text = "\n타워 A\n\n키보드 : Q", // 줄바꿈으로 정보 추가
                Location = new Point(520, 10),
                Size = new Size(80, 100), // 버튼 높이 증가
                Image = Properties.Resources.RtowerA, // Resources에 저장된 이미지 이름 사용
                TextImageRelation = TextImageRelation.ImageAboveText, // 이미지와 텍스트 위치 조정
                ImageAlign = ContentAlignment.TopCenter, // 이미지 정렬
                TextAlign = ContentAlignment.BottomCenter // 텍스트 정렬
            };
            towerAButton.Click += (s, e) =>
            {
                selectedTower = new TowerA();
                selectedTowerLabel.Text = "선택된 타워: 타워 A";
            };
            this.Controls.Add(towerAButton);

            // 타워 B 버튼
            Button towerBButton = new Button
            {
                Text = "\n타워 B\n\n키보드 : W", // 줄바꿈으로 정보 추가
                Location = new Point(520, 140),
                Size = new Size(80, 100), // 버튼 높이 증가
                Image = Properties.Resources.RtowerB, // Resources에 저장된 이미지 이름 사용
                TextImageRelation = TextImageRelation.ImageAboveText, // 이미지와 텍스트 위치 조정
                ImageAlign = ContentAlignment.TopCenter, // 이미지 정렬
                TextAlign = ContentAlignment.BottomCenter // 텍스트 정렬
            };
            towerBButton.Click += (s, e) =>
            {
                selectedTower = new TowerB();
                selectedTowerLabel.Text = "선택된 타워: 타워 B";
            };
            this.Controls.Add(towerBButton);

            // 타워 C 버튼
            Button towerCButton = new Button
            {
                Text = "\n타워 C\n\n키보드 : E", // 줄바꿈으로 정보 추가
                Location = new Point(520, 270),
                Size = new Size(80, 100), // 버튼 높이 증가
                Image = Properties.Resources.RtowerC, // Resources에 저장된 이미지 이름 사용
                TextImageRelation = TextImageRelation.ImageAboveText, // 이미지와 텍스트 위치 조정
                ImageAlign = ContentAlignment.TopCenter, // 이미지 정렬
                TextAlign = ContentAlignment.BottomCenter // 텍스트 정렬
            };
            towerCButton.Click += (s, e) =>
            {
                selectedTower = new TowerC();
                selectedTowerLabel.Text = "선택된 타워: 타워 C";
            };
            this.Controls.Add(towerCButton);

            // 생성 버튼
            Button buildButton = new Button
            {
                Text = "생성\n\nSpace Bar", // 줄바꿈으로 설명 추가
                Location = new Point(520, 400),
                Size = new Size(80, 60) // 버튼 높이 증가
            };
            buildButton.Click += BuildTower;
            this.Controls.Add(buildButton);

            // 포인트 라벨 생성 및 추가
            pointLabel = new Label
            {
                Text = $"포인트: {point}",
                Location = new Point(620, 10),
                AutoSize = true
            };
            this.Controls.Add(pointLabel);
        }
        //철거 버튼
        private void InitializeDemolishButton()
        {
            demolishButton = new Button
            {
                Text = "철거\n\nF4",
                Location = new Point(520, 490),
                Size = new Size(80, 60),        // 버튼 크기 설정
                Enabled = false // 초기 상태 비활성화
            };
            demolishButton.Click += DemolishTower;
            this.Controls.Add(demolishButton);
        }
        //타워 생성
        private void BuildTower(object sender, EventArgs e)
        {
            if (selectedTower == null)
            {
                MessageBox.Show("타워를 선택하세요!");
                return;
            }

            // 선택한 타워의 비용을 가져옴
            int requiredPoints = selectedTower.Cost;

            // 포인트가 부족한 경우 경고 메시지 표시
            if (point < requiredPoints)
            {
                MessageBox.Show("포인트가 부족합니다!");
                return;
            }

            // 선택한 타일이 유효한 경우 타워 배치
            if (selectedTile != null && selectedTile.BackColor == Color.Yellow)
            {
                ITower tower = selectedTower;
                tower.Sprite.Location = new Point(selectedTile.Location.X + 5, selectedTile.Location.Y + 5);
                mapPanel.Controls.Add(tower.Sprite);
                tower.Sprite.BringToFront();
                tower.Sprite.Click += Tower_Click;

                towers.Add(tower);
                InitializeTowerAttackTimer(tower);

                // 선택한 타일의 색상을 회색으로 변경
                selectedTile.BackColor = Color.White;
                selectedTower = null;

                // 포인트 차감 및 라벨 업데이트
                point -= requiredPoints;
                UpdateScoreLabel();

                // 타워 생성 후 선택 상태 초기화
                selectedTowerLabel.Text = "선택된 타워: 없음";

                // 포커스 제거: 폼에 포커스를 설정하여 타워에서 포커스를 없앰
                this.ActiveControl = null;
            }
            else
            {
                MessageBox.Show("타일을 선택하세요!");
            }
        }
        //맵에 생성된 타워 클릭 이벤트
        private void Tower_Click(object sender, EventArgs e)
        {
            if (sender is PictureBox towerSprite)
            {
                // 선택된 타워 찾기
                clickedTower = towers.FirstOrDefault(t => t.Sprite == towerSprite);

                if (clickedTower != null)
                {
                    // 빈 타일이 노란색으로 선택되어 있다면 흰색으로 복구
                    if (selectedTile != null)
                    {
                        selectedTile.BackColor = Color.White;
                        selectedTile = null; // 선택된 빈 타일 초기화
                    }

                    // 이전에 선택된 타워 타일이 빨간색이었다면 흰색으로 복구
                    if (lastSelectedTile != null)
                    {
                        lastSelectedTile.BackColor = Color.White;
                    }

                    // 현재 클릭된 타워의 위치에 해당하는 타일을 빨간색으로 표시
                    int tileRow = clickedTower.Sprite.Location.Y / 50;
                    int tileCol = clickedTower.Sprite.Location.X / 50;
                    mapTiles[tileRow, tileCol].BackColor = Color.Red;

                    // 선택된 타워 타일을 lastSelectedTile에 저장
                    lastSelectedTile = mapTiles[tileRow, tileCol];

                    // 철거 버튼 활성화
                    demolishButton.Enabled = true;
                }
            }
        }
        //맵을 클릭할 시 색 변화 이벤트
        private void mapTile_Click(object sender, EventArgs e)
        {
            if (sender is PictureBox clickedTile)
            {
                // 클릭된 타일이 경로 타일이거나 이미 타워가 설치된 타일이면 선택하지 않음
                if (clickedTile.BackColor == Color.LightBlue || clickedTile.BackColor == Color.Gray)
                    return;

                // 타워가 빨간색으로 선택되어 있다면 흰색으로 복구
                if (lastSelectedTile != null)
                {
                    lastSelectedTile.BackColor = Color.White;
                    lastSelectedTile = null; // 선택된 타워 초기화
                }

                // 이전에 선택한 빈 타일이 다른 타일이라면 흰색으로 복구
                if (selectedTile != null && selectedTile != clickedTile)
                {
                    selectedTile.BackColor = Color.White;
                }

                // 빈 타일 선택 여부 확인
                if (selectedTile == clickedTile)
                {
                    selectedTile.BackColor = Color.White;
                    selectedTile = null;
                }
                else
                {
                    selectedTile = clickedTile;
                    selectedTile.BackColor = Color.Yellow;
                }

                // 빈 타일을 클릭한 경우 철거 버튼 비활성화
                demolishButton.Enabled = false;
            }
        }
        // 타워 철거 이벤트
        private void DemolishTower(object sender, EventArgs e)
        {
            if (clickedTower != null)
            {
                // 타워 비용의 절반을 포인트로 가산
                int refundPoints = clickedTower.Cost / 2;
                point += refundPoints;
                pointLabel.Text = $"포인트: {point}";

                mapPanel.Controls.Remove(clickedTower.Sprite); // 맵에서 타워 제거
                clickedTower.Sprite.Dispose(); // 타워 스프라이트 제거

                // 타워의 타이머 중지 및 제거
                if (towerAttackTimers.ContainsKey(clickedTower))
                {
                    Timer attackTimer = towerAttackTimers[clickedTower];
                    attackTimer.Stop();
                    attackTimer.Dispose();
                    towerAttackTimers.Remove(clickedTower);
                }

                // 투사체 개수 데이터 초기화
                if (towerProjectileCount.ContainsKey(clickedTower))
                {
                    towerProjectileCount.Remove(clickedTower);
                }

                // 해당 타워와 연결된 모든 투사체 제거
                foreach (var projectile in projectiles.ToList())
                {
                    if (projectile.Tower == clickedTower)
                    {
                        projectile.RemoveFromGame(mapPanel); // UI에서 제거
                        projectiles.Remove(projectile); // 리스트에서 제거
                    }
                }

                int row = clickedTower.Sprite.Location.Y / 50;
                int col = clickedTower.Sprite.Location.X / 50;
                mapTiles[row, col].BackColor = Color.White; // 철거 후 타일 색상 복구
                clickedTower = null; // 철거된 타워 초기화

                // 철거 버튼 비활성화
                demolishButton.Enabled = false;
                MessageBox.Show($"타워 철거 완료! {refundPoints} 포인트가 환급되었습니다.");
            }
            else
            {
                MessageBox.Show("철거할 타워가 선택되지 않았습니다.");
            }
        }
    }
}