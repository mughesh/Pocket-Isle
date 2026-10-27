# Music (Lyria)

Drop Lyria tracks here (stereo; import rules make them Vorbis 0.5, Streaming). Name each after its cue, e.g.
`music.hub.wav`, then cut a seamless loop with **Tools > Pocket Isle > Audio Lab > Loop tool** (bakes `music.hub_loop.wav`
next to it) and run **Sync Cues**: every clip whose name starts with the cue id joins that cue. Untick the cue's
"Awaiting Clips" once it has the loop, and remove the un-looped original from the cue if both landed in it.
If the files are .mp3 / .ogg, add LFS rules for them to `.gitattributes` first (see `Assets/Docs/Audio/PhaseB-Hooks.md`).
