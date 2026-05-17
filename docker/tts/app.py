import os
import subprocess
import tempfile
from pathlib import Path

from fastapi import HTTPException
from fastapi import FastAPI
from fastapi.responses import JSONResponse, Response
from pydantic import BaseModel, Field

_piper_binary = Path(os.getenv("PIPER_BINARY", "/opt/piper/piper"))
_voice_model = Path(os.getenv("PIPER_VOICE_MODEL", "/voices/en_US-lessac-medium.onnx"))


class SynthesizeRequest(BaseModel):
    text: str = Field(min_length=1)


app = FastAPI(title="AiVoiceTest TTS", version="1.0.0")


def _piper_ready() -> bool:
    return _piper_binary.is_file() and _voice_model.is_file()


@app.get("/health")
def health():
    if not _piper_ready():
        return JSONResponse(
            status_code=503,
            content={
                "status": "unhealthy",
                "service": "tts",
                "piper_binary": str(_piper_binary),
                "voice_model": str(_voice_model),
            },
        )

    return {
        "status": "healthy",
        "service": "tts",
        "voice_model": _voice_model.name,
    }


@app.post("/v1/synthesize")
def synthesize(request: SynthesizeRequest):
    """Synthesize speech and return WAV bytes for the host client."""
    if not _piper_ready():
        raise HTTPException(status_code=503, detail="Piper is not ready")

    with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as output:
        output_path = output.name

    try:
        completed = subprocess.run(
            [
                str(_piper_binary),
                "--model",
                str(_voice_model),
                "--output_file",
                output_path,
            ],
            input=request.text,
            text=True,
            capture_output=True,
            check=False,
        )

        if completed.returncode != 0:
            detail = (completed.stderr or completed.stdout or "Piper failed").strip()
            raise HTTPException(status_code=500, detail=detail)

        with open(output_path, "rb") as audio_file:
            wav_bytes = audio_file.read()

        return Response(content=wav_bytes, media_type="audio/wav")
    except HTTPException:
        raise
    except OSError as ex:
        raise HTTPException(status_code=500, detail=str(ex)) from ex
    finally:
        if os.path.exists(output_path):
            os.remove(output_path)
