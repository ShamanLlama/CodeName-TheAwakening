class_name PlaytestClient
extends Node
## Sends only snapshots that the storage layer has acknowledged as durable.
var capture: PlaytestCapture
var platform: PlaytestPlatform
var persist: Callable
var notice := ""
var _request: HTTPRequest
var _sending := ""
var _prepared := ""
var _generation := -1
var _undo := {}
var _next_send := 0
var _flush_at := 0

func setup(core: PlaytestCapture, storage: PlaytestPlatform, save: Callable) -> void:
	capture = core
	platform = storage
	persist = save
	_flush_at = platform.now() + 20000

func _process(_delta: float) -> void:
	if capture == null: return
	platform.poll()
	if platform.failed:
		if not _undo.is_empty(): capture.undo_confirmation(_undo); _undo = {}
		capture.stop_capture("Storage is unavailable. Capture and sending stopped; export the local record.")
		cancel()
		return
	if not platform.writable: return
	# Expired local records are purged on opening, even without renewed consent.
	if not capture.corrupt and capture.expire(): persist.call()
	if not capture.confirmed: return
	if not _undo.is_empty():
		if platform.saved_generation >= _generation:
			_undo = {}
			notice = "Receipt confirmed and saved locally."
		else: return
	if not _sending.is_empty():
		if OS.has_feature("web"):
			for response in platform.responses(): _completed(response.id, int(response.status), response.body, response.retry_after)
		return
	if not _prepared.is_empty():
		if platform.saved_generation >= _generation:
			var job := capture.find_job(_prepared)
			_prepared = ""
			if not job.is_empty(): _send(job)
		return
	var now := platform.now()
	if not capture.data.staging.is_empty() and (capture.urgent or capture.data.staging.size() >= 32 or now >= _flush_at):
		capture.seal_batch()
		_generation = persist.call()
		_flush_at = now + 20000
	if now < _next_send: return
	var job := capture.next_job()
	if job.is_empty(): return
	capture.attempted(job)
	_prepared = job.id
	_generation = persist.call()
	if _generation < 0:
		_prepared = ""
		capture.stop_capture("Saving failed. No playtest request was sent.")

func _send(job: Dictionary) -> void:
	_sending = job.id
	_next_send = platform.now() + 5000
	notice = "Sending pending records…"
	if OS.has_feature("web"):
		platform.send(job.id, job.endpoint, job.body)
		return
	_request = HTTPRequest.new()
	_request.timeout = 10
	_request.body_size_limit = 16384
	_request.max_redirects = 0
	add_child(_request)
	_request.request_completed.connect(func(result: int, code: int, headers: PackedStringArray, body: PackedByteArray) -> void:
		var retry := ""
		for header in headers:
			if header.to_lower().begins_with("retry-after:"): retry = header.substr(12).strip_edges()
		_completed(job.id, code if result == HTTPRequest.RESULT_SUCCESS else 0, body.get_string_from_utf8(), retry))
	if _request.request(job.endpoint, ["Content-Type: application/json"], HTTPClient.METHOD_POST, job.body) != OK:
		_completed(job.id, 0, "", "")

func _completed(id: String, status: int, body: String, retry_after: String) -> void:
	if id != _sending: return
	_sending = ""
	if _request != null: _request.queue_free(); _request = null
	var job := capture.find_job(id)
	if job.is_empty(): return
	if status in [200, 201]:
		var receipt = JSON.parse_string(body)
		_undo = capture.confirm_job(job, receipt) if typeof(receipt) == TYPE_DICTIONARY else {}
		if _undo.is_empty():
			capture.postpone(job)
			notice = "The server did not provide a valid receipt. Records remain pending."
		else: notice = "Saving the server receipt…"
	elif status == 413 and capture.split_job(job):
		notice = "Splitting the pending batch; event identifiers are unchanged."
	elif status == 0 or status == 429 or status >= 500:
		capture.postpone(job, retry_after_seconds(retry_after))
		notice = "Connection unavailable or collection paused. Records are saved for retry."
	else:
		capture.reject(job, "Collection rejected this run (HTTP %d). Export the local record and contact the GTC operator." % status)
		notice = capture.warning
	_generation = persist.call()
	if _generation < 0 and not _undo.is_empty():
		capture.undo_confirmation(_undo)
		_undo = {}
		capture.stop_capture("The receipt could not be saved. Records remain available for export.")

static func retry_after_seconds(value: String) -> float:
	if value.is_valid_float(): return maxf(0, value.to_float())
	# HTTP dates are GMT, e.g. Wed, 21 Oct 2015 07:28:00 GMT.
	var parts := value.split(" ", false)
	var months := ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
	if parts.size() != 6 or not months.has(parts[2]): return 0
	var date := "%s-%02d-%02dT%s" % [parts[3], months.find(parts[2]) + 1, int(parts[1]), parts[4]]
	return maxf(0, Time.get_unix_time_from_datetime_string(date) - Time.get_unix_time_from_system())

func cancel() -> void:
	platform.cancel()
	if _request != null: _request.cancel_request(); _request.queue_free(); _request = null
	_sending = ""
	_prepared = ""
	_undo = {}
