﻿using UnityEngine;
using Modding;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using ReflectionHelper = Modding.ReflectionHelper;

namespace FiveKnights
{
    public class WDController : MonoBehaviour
    {
        private HealthManager _hm;
        private PlayMakerFSM _fsm;
        public GameObject dd; 
        private tk2dSpriteAnimator _tk;
        public static MusicPlayer CustomAudioPlayer;
        public static bool alone;
        private bool HIT_FLAG;
        public static WDController Instance;

        private IEnumerator Start()
        {
            Instance = this;
            _hm = dd.GetComponent<HealthManager>();
            _fsm = dd.LocateMyFSM("Dung Defender");
            _tk = dd.GetComponent<tk2dSpriteAnimator>();
            FiveKnights.preloadedGO["WD"] = dd;
            alone = true;
            OnDestroy();
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            ModHooks.Instance.BeforePlayerDeadHook += BeforePlayerDied;
            On.MusicCue.GetChannelInfo += MusicCue_GetChannelInfo;
            string dret = PlayerData.instance.dreamReturnScene;
            PlayerData.instance.dreamReturnScene = (dret == "Waterways_13") ? dret : "White_Palace_09";

            //Be sure to do CustomWP.Instance.wonLastFight = true; on win
            if (CustomWP.boss == CustomWP.Boss.Isma)
            {
                yield return null;
                dd.SetActive(false);
                FightController.Instance.CreateIsma();
                IsmaController ic = FiveKnights.preloadedGO["Isma2"].GetComponent<IsmaController>();
                ic.onlyIsma = true;
                yield return new WaitWhile(() => ic != null);
                //var endCtrl = GameObject.Find("Boss Scene Controller").LocateMyFSM("Dream Return");
                //endCtrl.SendEvent("DREAM RETURN");
                if (CustomWP.Instance.wonLastFight)
                {
                    int lev = CustomWP.Instance.lev + 1;
                    var box = (object) FiveKnights.Instance.Settings.CompletionIsma;
                    var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.Settings.CompletionIsma = (BossStatue.Completion) box;
                }
                var bossSceneController = GameObject.Find("Boss Scene Controller");
                var bsc = bossSceneController.GetComponent<BossSceneController>();
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.DoDreamReturn();
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Ogrim)
            {
                alone = false;
                _hm.hp = 950;
                _fsm.GetAction<Wait>("Rage Roar", 9).time = 1.5f;
                _fsm.FsmVariables.FindFsmBool("Raged").Value = true;
                yield return new WaitForSeconds(1f);
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                yield return new WaitWhile(() => _hm.hp > 600);
                _fsm.ChangeTransition("Rage Roar", "FINISHED", "Music");
                _fsm.ChangeTransition("Music", "FINISHED", "Set Rage");
                var ac1 = _fsm.GetAction<TransitionToAudioSnapshot>("Music", 1).snapshot;
                var ac2 = _fsm.GetAction<ApplyMusicCue>("Music", 2).musicCue;
                _fsm.AddAction("Rage Roar", new TransitionToAudioSnapshot()
                {
                    snapshot = ac1,
                    transitionTime = 0
                });
                _fsm.AddAction("Rage Roar", new ApplyMusicCue()
                {
                    musicCue = ac2,
                    transitionTime = 0,
                    delayTime = 0
                });
                HIT_FLAG = false;
                yield return new WaitWhile(() => !HIT_FLAG);
                PlayerData.instance.isInvincible = true;
                HeroController.instance.RelinquishControl();
                GameManager.instance.playerData.disablePause = true;
                _fsm.SetState("Stun Set");
                yield return new WaitWhile(() => _fsm.ActiveStateName != "Stun Land");
                _fsm.enabled = false;
                FightController.Instance.CreateIsma();
                IsmaController ic = FiveKnights.preloadedGO["Isma2"].GetComponent<IsmaController>();
                yield return new WaitWhile(() => !ic.introDone);
                _fsm.enabled = true;
                _fsm.SetState("Stun Recover");
                yield return null;
                yield return new WaitWhile(() => _fsm.ActiveStateName == "Stun Recover");
                CustomAudioPlayer.Volume = 1f;
                CustomAudioPlayer.UpdateMusic();
                _fsm.SetState("Rage Roar");
                PlayerData.instance.isInvincible = false;
                GameManager.instance.playerData.disablePause = false;
                PlayMakerFSM burrow = GameObject.Find("Burrow Effect").LocateMyFSM("Burrow Effect");
                yield return new WaitWhile(() => burrow.ActiveStateName != "Burrowing");
                burrow.SendEvent("BURROW END");
                yield return new WaitWhile(() => ic != null);
                if (CustomWP.Instance.wonLastFight)
                {
                    int lev = CustomWP.Instance.lev + 1;
                    var box = (object) FiveKnights.Instance.Settings.CompletionIsma2;
                    var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.Settings.CompletionIsma2 = (BossStatue.Completion) box;
                }
                PlayMakerFSM pm = GameCameras.instance.tk2dCam.gameObject.LocateMyFSM("CameraFade");
                pm.SendEvent("FADE OUT INSTANT");
                PlayMakerFSM fsm2 = GameObject.Find("Blanker White").LocateMyFSM("Blanker Control");
                fsm2.FsmVariables.FindFsmFloat("Fade Time").Value = 0;
                fsm2.SendEvent("FADE IN");
                yield return null;
                HeroController.instance.MaxHealth();
                yield return null;
                GameCameras.instance.cameraFadeFSM.FsmVariables.FindFsmBool("No Fade").Value = true;
                yield return null;
                GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
                {
                    SceneName = "White_Palace_09",
                    EntryGateName = "door_dreamReturnGGstatueStateIsma_GG_Statue_ElderHu(Clone)(Clone)",
                    Visualization = GameManager.SceneLoadVisualizations.GodsAndGlory,
                    WaitForSceneTransitionCameraFade = false,
                    PreventCameraFadeOut = true,
                    EntryDelay = 0

                });

                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Dryya)
            {
                dd.SetActive(false);
                DryyaSetup dc = FightController.Instance.CreateDryya();
                yield return new WaitWhile(() => dc != null);
                if (CustomWP.Instance.wonLastFight)
                {
                    int lev = CustomWP.Instance.lev + 1;
                    var box = (object) FiveKnights.Instance.Settings.CompletionDryya;
                    var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.Settings.CompletionDryya = (BossStatue.Completion) box;
                }
                yield return new WaitForSeconds(5.0f);
                var bossSceneController = GameObject.Find("Boss Scene Controller");
                var bsc = bossSceneController.GetComponent<BossSceneController>();
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.DoDreamReturn();
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Hegemol)
            {
                yield return null;
                dd.SetActive(false);
                HegemolController hegemolCtrl = FightController.Instance.CreateHegemol();
                GameObject.Find("Burrow Effect").SetActive(false);
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                yield return new WaitWhile(() => hegemolCtrl != null);
                if (CustomWP.Instance.wonLastFight)
                {
                    int lev = CustomWP.Instance.lev + 1;
                    var box = (object) FiveKnights.Instance.Settings.CompletionHegemol;
                    var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.Settings.CompletionHegemol = (BossStatue.Completion) box;
                }
                var bossSceneController = GameObject.Find("Boss Scene Controller");
                var bsc = bossSceneController.GetComponent<BossSceneController>();
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.DoDreamReturn();
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Ze || CustomWP.boss == CustomWP.Boss.Mystic)
            {
                Modding.Logger.Log("BOSS IS " + CustomWP.boss);
                yield return null;
                dd.SetActive(false);
                GameObject.Find("Burrow Effect").SetActive(false);
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                ZemerController zc = FightController.Instance.CreateZemer();
                GameObject zem = zc.gameObject;
                yield return new WaitWhile(() => zc != null);
                ZemerControllerP2 zc2 = zem.GetComponent<ZemerControllerP2>();
                yield return new WaitWhile(() => zc2 != null);
                if (CustomWP.Instance.wonLastFight)
                {
                    int lev = CustomWP.Instance.lev + 1;
                    if (CustomWP.boss == CustomWP.Boss.Ze)
                    {
                        var box = (object) FiveKnights.Instance.Settings.CompletionZemer;
                        var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                        fi.SetValue(box, true);
                        FiveKnights.Instance.Settings.CompletionZemer = (BossStatue.Completion) box;
                    }
                    else
                    {
                        var box = (object) FiveKnights.Instance.Settings.CompletionZemer2;
                        var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                        fi.SetValue(box, true);
                        FiveKnights.Instance.Settings.CompletionZemer2 = (BossStatue.Completion) box;
                    }
                }
                var bossSceneController = GameObject.Find("Boss Scene Controller");
                var bsc = bossSceneController.GetComponent<BossSceneController>();
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.DoDreamReturn();

                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.All)
            {
                alone = false;
                _hm.hp = 950;
                _fsm.GetAction<Wait>("Rage Roar", 9).time = 1.5f;
                _fsm.FsmVariables.FindFsmBool("Raged").Value = true;
                yield return new WaitForSeconds(1f);
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                yield return new WaitWhile(() => _hm.hp > 600);
                HIT_FLAG = false;
                yield return new WaitWhile(() => !HIT_FLAG);
                PlayerData.instance.isInvincible = true;
                HeroController.instance.RelinquishControl();
                GameManager.instance.playerData.disablePause = true;
                _fsm.SetState("Stun Set");
                yield return new WaitWhile(() => _fsm.ActiveStateName != "Stun Land");
                _fsm.enabled = false;
                FightController.Instance.CreateIsma();
                IsmaController ic = FiveKnights.preloadedGO["Isma2"].GetComponent<IsmaController>();
                yield return new WaitWhile(() => !ic.introDone);
                _fsm.enabled = true;
                _fsm.SetState("Stun Recover");
                yield return null;
                yield return new WaitWhile(() => _fsm.ActiveStateName == "Stun Recover");
                _fsm.SetState("Rage Roar");
                PlayerData.instance.isInvincible = false;
                GameManager.instance.playerData.disablePause = false;
                yield return new WaitWhile(() => ic != null);
                Destroy(this);
            }
            /*
             GameObject dryyaSilhouette = GameObject.Find("Silhouette Dryya");
                dryyaSilhouette.GetComponent<SpriteRenderer>().sprite = ArenaFinder.sprites["Dryya_Silhouette_1"];
                yield return new WaitForSeconds(0.125f);
                dryyaSilhouette.GetComponent<SpriteRenderer>().sprite = ArenaFinder.sprites["Dryya_Silhouette_2"];
                yield return new WaitForSeconds(0.125f);
                Destroy(dryyaSilhouette);
                yield return new WaitForSeconds(0.5f);
                FightController.Instance.CreateDryya();
                FightController.Instance.CreateIsma();
                */
        }

        public void BeforePlayerDied()
        {
            Log("RAN");
            CustomAudioPlayer.StopMusic();
        }
        
        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("White Defender"))
            {
                HIT_FLAG = true;
            }
            orig(self, hitInstance);
        }

        private void OnCollisionEnter2D(Collision2D c)
        {
            if (!_tk.IsPlaying("Roll")) return;
            if (c.gameObject.layer == 8 && c.gameObject.name.Contains("Front"))
            {
                _fsm.SetState("RJ Wall");
            }
        }

        public void PlayMusic(AudioClip clip, float vol = 0f)
        {
            MusicCue musicCue = ScriptableObject.CreateInstance<MusicCue>();
            List<MusicCue.MusicChannelInfo> channelInfos = new List<MusicCue.MusicChannelInfo>();
            MusicCue.MusicChannelInfo channelInfo = new MusicCue.MusicChannelInfo();
            channelInfo.SetAttr("clip", clip);
            channelInfos.Add(channelInfo);
            musicCue.SetAttr("channelInfos", channelInfos.ToArray());
            GameManager.instance.AudioManager.ApplyMusicCue(musicCue, 0, 0, false);
        }
        
        /*public void PlayMusic(string clip, float vol = 0f)
        {
            GameObject actor = GameObject.Find("Audio Player Actor");
            AudioClip ac = ArenaFinder.Clips[clip];
            CustomAudioPlayer = new MusicPlayer
            {
                Volume = vol,
                Clip = ac,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Loop = true,
                Spawn = HeroController.instance.gameObject
            };
            CustomAudioPlayer.DoPlayRandomClip();
        }*/

        private bool startedMusic;
        
        private MusicCue.MusicChannelInfo MusicCue_GetChannelInfo(On.MusicCue.orig_GetChannelInfo orig, MusicCue self, MusicChannels channel)
        {
            if (!startedMusic && CustomWP.boss == CustomWP.Boss.Ogrim && self.name.Contains("Defender"))
            {
                startedMusic = true;
                PlayMusic(ArenaFinder.Clips["IsmaMusic"]);
            }
            else if (self.name.Contains("Defender"))
            {
                return null;
            }
            return orig(self, channel);
        }

        private void OnDestroy()
        {
            ModHooks.Instance.BeforePlayerDeadHook -= BeforePlayerDied;
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.MusicCue.GetChannelInfo -= MusicCue_GetChannelInfo;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[White Defender] " + o);
        }
    }
}