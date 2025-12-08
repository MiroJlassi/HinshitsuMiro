import express from "express";
import dotenv from "dotenv";
import fs from "fs/promises";
import path from "path";

// Services
import { runLighthouse } from "./Services/lighthouseService.js"; 
import { convertToToon } from "./Services/toonService.js";
import { GptService } from "./Services/gptService.js";

dotenv.config();

const app = express();
app.use(express.json());

const resultsDir = path.join(process.cwd(), 'results');
fs.mkdir(resultsDir, { recursive: true }).catch(console.error);

// Health check / root endpoint
app.get("/", (req, res) => {
  res.json({ 
    status: "running",
    service: "Lighthouse Performance Analyzer",
    environment: process.env.NODE_ENV || "development",
    port: process.env.PORT || 3000,
    platform: process.platform,
    endpoints: {
      analyze: "POST /analyze - Analyze a URL with Lighthouse"
    }
  });
});

app.post("/analyze", async (req, res) => {
  const { url } = req.body;
  if (!url) return res.status(400).json({ error: "URL is required" });

  console.log(`\n--- Starting Analysis for: ${url} ---`);

  try {
    console.log("1. Running Lighthouse...");
    const lighthouseRaw = await runLighthouse(url);
    
    console.log("2. Compressing data to TOON format...");
    const toonData = convertToToon(lighthouseRaw);

    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    await fs.writeFile( // local saving for now
      path.join(resultsDir, `toon_${timestamp}.json`), 
      JSON.stringify(toonData, null, 2)
    );

    console.log("3. Sending to Azure OpenAI...");
    const gptService = new GptService();
    const aiAnalysis = await gptService.analyzeStructured(toonData);

    console.log("4. Done.");
    res.json({
      meta: {
        url,
        timestamp: new Date().toISOString(),
        lighthouseScore: toonData.scores
      },
      analysis: aiAnalysis,
      stats: toonData,
      rawReport: lighthouseRaw
    });

  } catch (error) {
    console.error("Analysis Failed:", error);
    
    // Check if it's a Chrome/environment issue
    // this should never happens now with already installed chrome
      const isChromeError = error.message?.includes('Chrome') || 
                          error.message?.includes('Chromium') ||
                          error.code === 'ERR_LAUNCHER_NOT_INSTALLED';
    
    if (isChromeError) {
      return res.status(503).json({ 
        error: "Service Unavailable", 
        message: "Lighthouse analysis requires Chrome/Chromium which is not available in this environment.",
        details: error.message,
        platform: process.platform,
        suggestion: "Please deploy to Azure App Service Linux, Azure Container Apps, or use a Docker container with Chrome pre-installed."
      });
    }
    
    res.status(500).json({ 
      error: "Analysis Failed", 
      details: error.message,
      stack: process.env.NODE_ENV === 'development' ? error.stack : undefined
    });
  }
});

const PORT = process.env.PORT || process.env.port || 3000;
app.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
  console.log(`Environment: ${process.env.NODE_ENV || 'development'}`);
  console.log(`Platform: ${process.platform}`);
});