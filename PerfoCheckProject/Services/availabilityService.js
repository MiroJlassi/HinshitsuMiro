import axios from "axios";

export async function checkAvailability(url) {
  const start = Date.now();

  try {
    const response = await axios.get(url);
    // miro here: this is a placeholder for future api implementation for global availability check (for now just a simple up/down check)
    return {
      isUp: true,
      statusCode: response.status,
      responseTimeMs: Date.now() - start,
    };
  } catch (err) {
    return {
      isUp: false,
      statusCode: err.response?.status ?? null,
      responseTimeMs: Date.now() - start,
    };
  }
}
