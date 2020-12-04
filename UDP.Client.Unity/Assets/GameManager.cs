using ENet;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UDP.Core.Model;
using UDP.Core.Model.Packet;
using UDP.Core.Model.Packet.Contract;
using UDP.Core.Model.Packet.Enum;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private Host _client;
    private Peer _peer;

    public Sprite[] raw_images;
    public Tuple<EValue, Sprite>[] images;

    const int channelID = 0;

    private Button[] element_buttons;
    private EValue[] board;
    private string LocalPlayerName = "DefaultName";

    Text Player1Name;
    Text Player2Name;
    Text Player1Score;
    Text Player2Score;

    GameObject waiting_pannel;
    Coroutine waiting_animation;

    uint playerId;

    private void Awake()
    {
        images = new Tuple<EValue, Sprite>[]
        {
            new Tuple<EValue, Sprite>(EValue.EMPTY, raw_images[0]),
            new Tuple<EValue, Sprite>(EValue.O, raw_images[1]),
            new Tuple<EValue, Sprite>(EValue.X, raw_images[2]),
        };

        Application.runInBackground = true;

        board = new EValue[]
        {
            EValue.EMPTY, EValue.EMPTY, EValue.EMPTY,
            EValue.EMPTY, EValue.EMPTY, EValue.EMPTY,
            EValue.EMPTY, EValue.EMPTY, EValue.EMPTY,
        };

        InitializeUI();
    }

    void InitializeUI()
    {
        var buttons = gameObject.GetComponentsInChildren<Button>(includeInactive: true);
        var texts = gameObject.GetComponentsInChildren<Text>(includeInactive: true);
        var inputFields = gameObject.GetComponentsInChildren<InputField>(includeInactive: true);
        var menuItems = gameObject.GetComponentsInChildren<RectTransform>(includeInactive: true).Select(x => x.gameObject);

        Player1Name = texts.Single(x => x.name == "PlayerName_0");
        Player2Name = texts.Single(x => x.name == "PlayerName_1");
        Player1Score = texts.Single(x => x.name == "PlayerScore_0");
        Player2Score = texts.Single(x => x.name == "PlayerScore_1");

        element_buttons = buttons.Where(x => x.name.Contains("grid_element")).ToArray();

        Button Play = buttons.Single(x => x.name == "PlayButton");
        var Menu = menuItems.Single(x => x.name == "Menu");
        var GameGrid = menuItems.Single(x => x.name == "GameGrid");
        var IpPortInput = inputFields.Single(x => x.name == "ServerConnectionInput");

        IpPortInput.onValueChanged.AddListener((value) =>
        {
            var match = Regex.Match(value, @"\b(\d{1,3}\.){3}\d{1,3}\:\d{1,8}\b");
            Play.interactable = match.Success || string.IsNullOrEmpty(value);
        });

        var NameInput = inputFields.Single(x => x.name == "PlayerNameInput");
        NameInput.onValueChanged.AddListener((value) =>
        {
            LocalPlayerName = string.IsNullOrEmpty(value) ? $"Default{UnityEngine.Random.Range(0, 999)}" : value;
            NameInput.GetComponentsInChildren<Text>().Single(x => x.name == "Placeholder").text = LocalPlayerName;
        });

        var Header = menuItems.Single(x => x.name == "Header");
        var Footer = menuItems.Single(x => x.name == "Footer");
        var Disconnect = buttons.Single(x => x.name == "Disconnect");
        var Waiting = menuItems.Single(x => x.name == "WaitPannel");

        waiting_pannel = Waiting;
        Play.onClick.AddListener(() =>
        {
            InitENet(IpPortInput.text);
            Menu.SetActive(false);
            GameGrid.SetActive(true);
            Header.SetActive(true);
            Footer.SetActive(true);
            Disconnect.gameObject.SetActive(true);
            Waiting.SetActive(true);

            var waitingText = Waiting.GetComponentInChildren<Text>();
            waiting_animation = StartCoroutine(WaitingTextAnimation(waitingText));
        });

        Button Exit = buttons.Single(x => x.name == "ExitButton");
        Exit.onClick.AddListener(() =>
        {
            Application.Quit();
        });

        Disconnect.onClick.AddListener(() =>
        {
            StopAllCoroutines();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        foreach (Button button in element_buttons)
        {
            int buttonId = int.Parse(button.name.Split('_').Last());
            button.onClick.AddListener(() =>
            {
                ElementClicked(buttonId);
                Debug.Log($"Clicked {buttonId}");
            });
        }
    }

    void ElementClicked(int id)
    {
        PlayerInput playerInput = default;
        playerInput.PacketId = EPacketId.BoardUpdateRequest;
        playerInput.BoardPosition = (uint)id;

        var protocol = new Protocol();

        byte[] buffer = protocol.Serialize(playerInput);
        var packet = default(Packet);

        packet.Create(buffer);
        _peer.Send(channelID, ref packet);
    }

    private IEnumerator UpdateENet()
    {
        for (; ; )
        {
            yield return new WaitForSeconds(0.1f);

            bool polled = false;
            while (!polled)
            {
                if (_client.CheckEvents(out ENet.Event netEvent) <= 0)
                {
                    if (_client.Service(15, out netEvent) <= 0)
                        break;
                    polled = true;
                }

                switch (netEvent.Type)
                {
                    case ENet.EventType.None:
                        break;
                    case ENet.EventType.Connect:
                        Debug.Log("Client connected to server - ID: " + _peer.ID);
                        SendLogin();
                        break;
                    case ENet.EventType.Disconnect:
                        Debug.Log("Client disconnected from server");
                        RestartGame();
                        break;
                    case ENet.EventType.Timeout:
                        Debug.Log("Client connection timeout");
                        RestartGame();
                        break;
                    case ENet.EventType.Receive:
                        Debug.Log("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                        ParsePacket(ref netEvent);
                        netEvent.Packet.Dispose();
                        break;
                }
            }
        }
    }

    private void ParsePacket(ref ENet.Event netEvent)
    {
        Protocol protocol = new Protocol();
        var readBuffer = new byte[1024];

        netEvent.Packet.CopyTo(readBuffer);
        protocol.Deserialize(readBuffer, out PacketInfo packetInfo);

        Debug.Log("ParsePacket received: " + packetInfo.PacketId);
        
        if (packetInfo.PacketId == EPacketId.LoginResponse)
        {
            protocol.Deserialize(readBuffer, out Login login);
            playerId = login.PlayerId;
            Debug.Log(login);
        }
        else if (packetInfo.PacketId == EPacketId.LoginEvent)
        {
            protocol.Deserialize(readBuffer, out Login login);

            Player2Name.text = string.Concat(login.PlayerName);
            Player2Score.text = "0";

            Player1Name.text = LocalPlayerName;
            Player1Score.text = "0";

            waiting_pannel.SetActive(false);
            StopCoroutine(waiting_animation);

            Debug.Log($"OtherPlayerId: { login.PlayerId }, OtherPlayerName: { login.PlayerName }");
        }
        else if (packetInfo.PacketId == EPacketId.BoardUpdateEvent)
        {
            protocol.Deserialize(readBuffer, out GameUpdate gameUpdate);
            Player1Score.text = $"{gameUpdate.Player1Score}";
            Player2Score.text = $"{gameUpdate.Player2Score}";
            UpdateBoard(gameUpdate.Board);
        }
        else if (packetInfo.PacketId == EPacketId.LogoutEvent)
        {
            protocol.Deserialize(readBuffer, out Login login);
            Debug.Log($"{string.Concat(login.PlayerName)} was disconnected!");
        }
    }

    private void SendLogin()
    {
        Debug.Log("SendLogin");
        var protocol = new Protocol();
        Login login = default;

        login.PacketId = EPacketId.LoginRequest;
        login.PlayerId = 0;
        login.PlayerName = LocalPlayerName.ToCharArray();

        var buffer = protocol.Serialize(login);
        var packet = default(Packet);
        packet.Create(buffer);
        _peer.Send(channelID, ref packet);
    }

    private void InitENet(string connectionString)
    {
        string ip = "127.0.0.1";
        ushort port = 6005;

        if (!string.IsNullOrEmpty(connectionString))
        {
            var match = Regex.Match(connectionString, @"\b(\d{1,3}\.){3}\d{1,3}\:\d{1,8}\b");

            if (!match.Success) return;
            var splitConnectionString = connectionString.Split(':');

            ip = splitConnectionString[0];
            port = ushort.Parse(splitConnectionString[1]);
        }

        Library.Initialize();
        _client = new Host();
        Address address = new Address();

        address.SetHost(ip);
        address.Port = port;
        _client.Create();
        Debug.Log("Connecting");
        _peer = _client.Connect(address);

        StartCoroutine(UpdateENet());
    }

    private void UpdateBoard(EValue[] board)
    {
        for (int i = 0; i < board.Length; i++)
        {
            element_buttons[i].image.sprite = images.Single(x => x.Item1 == board[i]).Item2;
            this.board[i] = board[i];
        }
    }

    IEnumerator WaitingTextAnimation(Text text)
    {
        string originalText = text.text.Trim('.');
        int qtty = 0;
        int max = 3;
        for (; ; )
        {
            yield return new WaitForSeconds(0.5f);
            qtty = qtty >= max ? 0 : qtty + 1;
            text.text = originalText;
            for (int i = 0; i < qtty; i++)
                text.text += ".";
        }
    }

    void RestartGame()
    {
        StopAllCoroutines();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnDestroy()
    {
        if (_client == null) return;

        _client.Dispose();
        Library.Deinitialize();
    }
}
