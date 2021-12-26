﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Photon.Pun;
using TMPro;

public class GameManager : MonoBehaviourPun {
    public static GameManager Instance { get; private set; }

    [SerializeField] AudioClip intro, loop, invincibleIntro, invincibleLoop, megaMushroomLoop;

    public int levelMinTileX, levelMinTileY, levelWidthTile, levelHeightTile;
    public Vector3 spawnpoint;
    public Tilemap tilemap, semiSolidTilemap;
    TileBase[] original;
    BoundsInt origin;
    GameObject currentStar = null;
    GameObject[] spawns;
    float spawnStarCount;
    SpriteRenderer spriteRenderer;
    new AudioSource audio;

    public GameObject localPlayer;
    public bool paused, loaded;
    [SerializeField] GameObject pauseUI, pauseButton;
    public bool gameover = false, musicEnabled = false;

    void Start() {
        Instance = this;
        audio = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        origin = new BoundsInt(levelMinTileX, levelMinTileY, 0, levelWidthTile, levelHeightTile, 1);

        TileBase[] map = tilemap.GetTilesBlock(origin);
        original = new TileBase[map.Length];
        for (int i = 0; i < map.Length; i++) {
            if (map[i] == null) continue;
            original[i] = map[i];
        }

        spawns = GameObject.FindGameObjectsWithTag("StarSpawn");
        
        SceneManager.SetActiveScene(gameObject.scene);
        localPlayer = PhotonNetwork.Instantiate("Prefabs/Player", spawnpoint, Quaternion.identity, 0);
        if (!localPlayer) {
            //not connected to a room, started scene through editor. spawn player
            PhotonNetwork.OfflineMode = true;
            PhotonNetwork.CreateRoom("debug");
            GameObject.Instantiate(Resources.Load("Prefabs/Static/GlobalController"), Vector3.zero, Quaternion.identity);
            localPlayer = PhotonNetwork.Instantiate("Prefabs/Player", spawnpoint, Quaternion.identity, 0);
        }
        Camera.main.GetComponent<CameraController>().target = localPlayer;
        localPlayer.GetComponent<Rigidbody2D>().isKinematic = true;
        localPlayer.GetComponent<PlayerController>().enabled = false;
        PhotonNetwork.IsMessageQueueRunning = true;
        photonView.RPC("IveFinishedLoading", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.NickName);
    }
    [PunRPC]
    public void IveFinishedLoading(string username) {
        Debug.Log(username + " has finished loading");
        GlobalController.Instance.loadedPlayers.Add(username);
    }
    public void LoadingComplete() {
        loaded = true;

        GameObject canvas = GameObject.FindGameObjectWithTag("LoadingCanvas");
        if (canvas) {
            canvas.GetComponent<Animator>().SetTrigger("loaded");
            canvas.GetComponent<AudioSource>().Stop();
        }
        StartCoroutine(WaitToActivate());
    }

    IEnumerator WaitToActivate() {
        yield return new WaitForSeconds(3.5f);
        GameManager.Instance.audio.PlayOneShot((AudioClip) Resources.Load("Sound/startgame")); 

        foreach (var wfgs in GameObject.FindObjectsOfType<WaitForGameStart>()) {
            wfgs.AttemptExecute();
        }
        localPlayer.GetComponent<Rigidbody2D>().isKinematic = false;
        localPlayer.GetComponent<PlayerController>().enabled = true;
        localPlayer.GetPhotonView().RPC("PreRespawn", RpcTarget.All);

        yield return new WaitForSeconds(1f);
        musicEnabled = true;
        GlobalController.Instance.loadedPlayers.Clear();
        SceneManager.UnloadSceneAsync("Loading");
    }

    IEnumerator EndGame(Photon.Realtime.Player winner) {
        audio.Stop();
        yield return new WaitForSecondsRealtime(1);
        GameObject text = GameObject.FindWithTag("wintext");
        text.GetComponent<Animator>().SetTrigger("start");
        text.GetComponent<TMP_Text>().text = winner.NickName + " Wins!";
        AudioMixer mixer = audio.outputAudioMixerGroup.audioMixer;
        mixer.SetFloat("MusicSpeed", 1f);
        mixer.SetFloat("MusicPitch", 1f);
        if (winner.IsLocal) {
            audio.PlayOneShot((AudioClip) Resources.Load("Sound/match-win"));
        } else {
            audio.PlayOneShot((AudioClip) Resources.Load("Sound/match-lose"));
        }
        //show results screen
        yield return new WaitForSecondsRealtime(4);
        SceneManager.LoadScene("MainMenu");
    }

    [PunRPC]
    public void Win(Photon.Realtime.Player winner) {
        gameover = true;
        StartCoroutine(EndGame(winner));
    }
    
    [PunRPC]
    void ModifyTilemap(int x, int y, string newtile) {
        Tilemap tm = GameManager.Instance.tilemap;
        Vector3Int loc = new Vector3Int(x,y,0);
        tm.SetTile(loc, (Tile) Resources.Load("Tilemaps/Tiles" + newtile));
    }
    [PunRPC]
    void SpawnBreakParticle(int x, int y, float r, float g, float b) {
        Transform tm = GameManager.Instance.tilemap.transform;
        GameObject particle = (GameObject) GameObject.Instantiate(Resources.Load("Prefabs/Particle/BrickBreak"), new Vector3(x * tm.localScale.x + tm.position.x + 0.25f, y * tm.localScale.y + tm.position.y + 0.25f, 0), Quaternion.identity);
        ParticleSystem system = particle.GetComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.startColor = new Color(r, g, b, 1);
    }
    
    [PunRPC]
    void BumpBlock(int x, int y, string newTile, int spawnResult, bool down) {
        Tilemap tm = GameManager.Instance.tilemap;
        Vector3Int loc = new Vector3Int(x,y,0);

        GameObject bump = (GameObject) GameObject.Instantiate(Resources.Load("Prefabs/Bump/BlockBump"), new Vector3(x*tm.transform.localScale.x+tm.transform.position.x+0.25f,y*tm.transform.localScale.y+tm.transform.position.y+0.25f,0), Quaternion.identity);
        BlockBump bb = bump.GetComponentInChildren<BlockBump>();

        bb.fromAbove = down;
        bb.resultTile = newTile;
        bb.sprite = tm.GetSprite(loc);
        bb.spawn = (BlockBump.SpawnResult) spawnResult;

        tm.SetTile(loc, null);
    }

    void Update() {
        
        if (gameover) return;

        foreach (var player in FindObjectsOfType<PlayerController>()) {
            if (player.GetComponent<PlayerController>().stars >= GlobalController.Instance.starRequirement) {
                //game over, losers
                photonView.RPC("Win", RpcTarget.All, player.photonView.Owner);
                return;
            }
        }

        if (!loaded) {
            if (PhotonNetwork.CurrentRoom == null || GlobalController.Instance.loadedPlayers.Count >= PhotonNetwork.CurrentRoom.PlayerCount) {
                LoadingComplete();
            }
            return;
        }
        if (musicEnabled) {
            HandleMusic();
        }


        if (currentStar == null) {
            
            if (PhotonNetwork.IsMasterClient) {
                if ((spawnStarCount -= Time.deltaTime) <= 0) {
                    Vector3 spawnPos = spawns[(int) (Random.value * spawns.Length)].transform.position;
                    //Check for people camping spawn
                    foreach (var hit in Physics2D.OverlapCircleAll(spawnPos, 4)) {
                        if (hit.gameObject.tag == "Player") {
                            //cant spawn here
                            return;
                        }
                    }

                    currentStar = PhotonNetwork.InstantiateRoomObject("Prefabs/BigStar", spawnPos, Quaternion.identity);
                    //TODO: star appear sound
                    spawnStarCount = 10f;
                }
            } else {
                currentStar = GameObject.FindGameObjectWithTag("bigstar");
            }
        }
    }

    void HandleMusic() {
        if (intro != null) {
            intro = null;
            audio.clip = intro;
            audio.loop = false;
            audio.Play();
        }

        if (localPlayer != null) {
            bool invincible = false;
            bool mega = false;
            foreach (PlayerController player in GameObject.FindObjectsOfType<PlayerController>()) {
                if (player.state == PlayerController.PlayerState.Giant) {
                    mega = true;
                    break;
                }
                if (player.invincible > 0) {
                    invincible = true;
                    break;
                }
            }

            if (mega) {
                if (audio.clip != megaMushroomLoop || !audio.isPlaying) {
                    audio.clip = megaMushroomLoop;
                    audio.loop = true;
                    audio.Play();
                }
            } else if (invincible) {
                if (audio.clip == intro || audio.clip == loop) {
                    audio.clip = invincibleIntro;
                    audio.loop = false;
                    audio.Play();
                }
                if (audio.clip == invincibleIntro && !audio.isPlaying) {
                    audio.clip = invincibleLoop;
                    audio.loop = true;
                    audio.Play();
                }
                return;
            } else if (!(audio.clip == intro || audio.clip == loop)) {
                audio.Stop();
                if (intro != null) {
                    audio.clip = intro;
                    audio.loop = false;
                    audio.Play();
                }
            }
        }

        if (!audio.isPlaying) {
            audio.clip = loop;
            audio.loop = true;
            audio.Play();
        }

        bool speedup = false;
        int required = GlobalController.Instance.starRequirement;
        foreach (var player in GameObject.FindGameObjectsWithTag("Player")) {
            int stars = player.GetComponent<PlayerController>().stars;
            if (((float) stars + 1) / required >= 0.9f) {
                speedup = true;
                break;
            }
        }

        AudioMixer mixer = audio.outputAudioMixerGroup.audioMixer;
        if (speedup) {
            mixer.SetFloat("MusicSpeed", 1.25f);
            mixer.SetFloat("MusicPitch", 1f / 1.25f);
        } else {
            mixer.SetFloat("MusicSpeed", 1f);
            mixer.SetFloat("MusicPitch", 1f);
        }
    }

    public void ResetTiles() {

        foreach (GameObject coin in GameObject.FindGameObjectsWithTag("coin")) {
            coin.GetComponent<SpriteRenderer>().enabled = true;
            coin.GetComponent<BoxCollider2D>().enabled = true;
        }
        
        tilemap.SetTilesBlock(origin, original);
        if (PhotonNetwork.IsMasterClient) {
            foreach (EnemySpawnpoint point in GameObject.FindObjectsOfType<EnemySpawnpoint>()) {
                point.AttemptSpawning();
            }
        }
    }

    public void Pause() {
        paused = !paused;
        Instance.pauseUI.SetActive(paused);
        EventSystem.current.SetSelectedGameObject(pauseButton);
    }

    public void Quit() {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("MainMenu");
    }

    public float GetLevelMinX() {
        return (levelMinTileX * tilemap.transform.localScale.x) + tilemap.transform.position.x;
    }
    public float GetLevelMinY() {
        return (levelMinTileY * tilemap.transform.localScale.y) + tilemap.transform.position.y;
    }
    public float GetLevelMaxX() {
        return ((levelMinTileX + levelWidthTile) * tilemap.transform.localScale.x) + tilemap.transform.position.x;
    }
    public float GetLevelMaxY() {
        return ((levelMinTileY + levelHeightTile) * tilemap.transform.localScale.y) + tilemap.transform.position.y;
    }


    public float size = 1.39f, ySize = 0.8f;
    public Vector3 GetSpawnpoint(int playerIndex, int players = -1) {
        if (players <= -1)
            players = PhotonNetwork.CurrentRoom.PlayerCount;
        float comp = ((float) playerIndex/players) * 2 * Mathf.PI + (Mathf.PI/2f) + (Mathf.PI/(2*players));
        float scale = (2-(players+1f)/players) * size;
        return spawnpoint + new Vector3(Mathf.Sin(comp) * scale, Mathf.Cos(comp) * (players > 2 ? scale * ySize : 0), 0);
    }
    [Range(1,10)]
    public int playersToVisualize = 10;
    void OnDrawGizmosSelected() {
        for (int i = 0; i < playersToVisualize; i++) {
            Gizmos.color = new Color(i/playersToVisualize, 0, 0, 0.75f);
            Gizmos.DrawCube(GetSpawnpoint(i, playersToVisualize) + Vector3.down/4f, Vector2.one/2f);
        }
    }
}
