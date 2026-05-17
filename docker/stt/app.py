import os
import tempfile
from contextlib import asynccontextmanager

from fastapi import FastAPI, File, HTTPException, UploadFile
from fastapi.responses import JSONResponse

_model = None
_model_name = os.getenv("WHISPER_MODEL", "small")
_device = os.getenv("WHISPER_DEVICE", "cpu")
_compute_type = os.getenv("WHISPER_COMPUTE_TYPE", "int8")


@asynccontextmanager
async def lifespan(_: FastAPI):
    global _model
    from faster_whisper import WhisperModel

    _model = WhisperModel(_model_name, device=_device, compute_type=_compute_type)
    yield
    _model = None


app = FastAPI(title="AiVoiceTest STT", version="1.0.0", lifespan=lifespan)


@app.get("/health")
def health():
    if _model is None:
        return JSONResponse(
            status_code=503,
            content={"status": "unhealthy", "service": "stt"},
        )

    return {
        "status": "healthy",
        "service": "stt",
        "model": _model_name,
        "device": _device,
    }


@app.post("/v1/transcribe")
async def transcribe(file: UploadFile = File(...)):
    """Stub contract for Phase 2; full wiring lands in Phase 3."""
    if _model is None:
        raise HTTPException(status_code=503, detail="STT model not loaded")

    if not file.filename:
        raise HTTPException(status_code=400, detail="Audio file is required")

    suffix = os.path.splitext(file.filename)[1] or ".wav"
    with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as temp:
        temp.write(await file.read())
        temp_path = temp.name

    try:
        segments, _info = _model.transcribe(temp_path)
        text = "".join(segment.text for segment in segments).strip()
        return {"text": text}
    finally:
        if os.path.exists(temp_path):
            os.remove(temp_path)
