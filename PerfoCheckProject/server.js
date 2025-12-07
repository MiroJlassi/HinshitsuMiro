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

app.get("/", (req, res) => {
  res.json({ 
    status: "running",
    service: "Lighthouse Performance Analyzer",
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
    res.status(500).json({ 
      error: "Analysis Failed", 
      details: error.message 
    });
  }
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Server running on port ${PORT}`));
