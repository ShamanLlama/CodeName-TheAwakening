class_name Sfx
extends Node
## Sound effects synthesised at startup, so the game ships without audio files.

const RATE := 22050

var volume := 0.4
var muted := false
var _streams: Dictionary = {}
var _players: Array[AudioStreamPlayer] = []


func _ready() -> void:
	_streams.click = _render(0.05, func(t: float) -> float: return sin(TAU * 900.0 * t) * exp(-t * 90.0) * 0.35)
	_streams.act = _render(0.32, func(t: float) -> float:
		return (sin(TAU * 110.0 * t) * 0.6 + sin(TAU * 165.0 * t + sin(TAU * 3.0 * t)) * 0.3) * exp(-t * 11.0) * minf(1.0, t * 200.0))
	_streams.refuse = _render(0.16, func(t: float) -> float: return sin(TAU * (190.0 - t * 300.0) * t) * exp(-t * 22.0) * 0.4)
	_streams.threat = _render(0.7, func(t: float) -> float:
		var tone := 98.0 if t < 0.28 else 87.0
		return sin(TAU * tone * t) * 0.55 * exp(-fmod(t, 0.28) * 7.0) * minf(1.0, t * 100.0))
	_streams.omen = _render(1.1, func(t: float) -> float:
		return (sin(TAU * 523.0 * t) * 0.35 + sin(TAU * 784.0 * t) * 0.22 + sin(TAU * 1046.5 * t * 1.003) * 0.12) * exp(-t * 3.2) * minf(1.0, t * 80.0))
	_streams.site = _render(1.4, func(t: float) -> float:
		return (sin(TAU * 65.4 * t) * 0.5 + sin(TAU * 98.0 * t) * 0.35 + sin(TAU * 130.8 * t) * 0.2) * exp(-t * 2.2) * minf(1.0, t * 30.0))
	_streams.end = _render(2.6, func(t: float) -> float:
		return (sin(TAU * 55.0 * t) * 0.6 + sin(TAU * 82.4 * t * (1.0 + 0.002 * sin(TAU * 0.7 * t))) * 0.4) * exp(-t * 1.1) * minf(1.0, t * 8.0))
	for i in 4:
		var player := AudioStreamPlayer.new()
		add_child(player)
		_players.append(player)


func _render(seconds: float, wave: Callable) -> AudioStreamWAV:
	var frames := int(seconds * RATE)
	var data := PackedByteArray()
	data.resize(frames * 2)
	for i in frames:
		data.encode_s16(i * 2, int(clampf(wave.call(i / float(RATE)), -1.0, 1.0) * 32000.0))
	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = RATE
	stream.data = data
	return stream


func play(sound: String) -> void:
	if muted or volume <= 0.0 or not _streams.has(sound):
		return
	for player in _players:
		if not player.playing:
			player.stream = _streams[sound]
			player.volume_db = linear_to_db(volume)
			player.play()
			return
