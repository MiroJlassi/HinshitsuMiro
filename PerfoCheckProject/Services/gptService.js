import OpenAI from "openai";
import dotenv from "dotenv";

dotenv.config();

export const AzureOpenAIOptions = {
  apiKey: process.env.AZURE_OPENAI_KEY || "",
  endpoint: process.env.AZURE_OPENAI_ENDPOINT || "",
  deployment: process.env.AZURE_GPT_DEPLOYMENT || "",
  apiVersion: process.env.AZURE_OPENAI_API_VERSION || "2024-02-15-preview",
  temperature: 1, // miro here this model version do not support other values for temperature
  timeoutSeconds: 60
};

const SYSTEM_PROMPT = `
You are a concise expert web performance & audit analyst. 
You will receive a summarized JSON dataset representing the top issues of a specific website category.

YOUR GOAL: 
Analyze the data and return a JSON object containing the top 3 critical observations and actionable advice.

RESPONSE SCHEMA (JSON ONLY):
{
  "summary": "<One concise sentence summarizing the health of this category>",
  "scoreInterpretation": "<Short explanation of the numeric score>",
  "topIssues": [
    "<Short specific issue 1 (e.g., '15 images missing alt tags')>",
    "<Short specific issue 2>",
    "<Short specific issue 3>"
  ],
  "recommendations": [
    "<Actionable step 1 for a developer>",
    "<Actionable step 2>",
    "<Actionable step 3>"
  ],
  "priority": "high|medium|low"
}

RULES:
- Do not respond with prose. JSON only.
- If the input lists "failureCount", mention the magnitude in "topIssues" (e.g., "High number of uncompressed images (24)").
- Keep recommendations technical and specific.
`;

export class GptService {
  constructor(options = AzureOpenAIOptions) {
    if (!options.apiKey || !options.endpoint) {
      throw new Error("Azure OpenAI API key and endpoint are required");
    }

    this.options = options;
    const baseEndpoint = options.endpoint.replace(/\/$/, "");
    const baseURL = `${baseEndpoint}/openai/deployments/${options.deployment}`;

    this.client = new OpenAI({
      apiKey: options.apiKey,
      baseURL: baseURL,
      defaultQuery: { "api-version": options.apiVersion },
      defaultHeaders: { "api-key": options.apiKey }
    });
  }

  async analyzeCategory(categoryName, inputData) {
    if (!inputData || (Array.isArray(inputData.issues) && inputData.issues.length === 0)) {
      return {
        summary: `No critical issues found for ${categoryName}.`,
        scoreInterpretation: "Excellent score.",
        topIssues: [],
        recommendations: ["Maintain current standards."],
        priority: "low"
      };
    }

    const userContent = `Category: ${categoryName}\n\nData Summary:\n${JSON.stringify(inputData, null, 2)}`;

    try {
      const resp = await this.client.chat.completions.create({
        messages: [
          { role: "system", content: SYSTEM_PROMPT },
          { role: "user", content: userContent }
        ],
        response_format: { type: "json_object" }
      });

      const raw = resp.choices?.[0]?.message?.content || "{}";
      return JSON.parse(raw);
    } catch (error) {
      console.error(`GPT Error (${categoryName}):`, error.message);
      return { 
        summary: "Analysis failed", 
        topIssues: ["AI Error"], 
        recommendations: [], 
        priority: "low" 
      };
    }
  }

  async analyzeStructured(toon) {    
    // 1. Performance Chunk
    const perfData = {
      score: toon.scores.performance,
      vitals: toon.vitals,
      opportunities: toon.performance.topOpportunities
    };

    // 2. Accessibility Chunk
    const accData = {
      score: toon.scores.accessibility,
      issues: toon.accessibility
    };

    // 3. SEO Chunk
    const seoData = {
      score: toon.scores.seo,
      issues: toon.seo
    };

    // 4. Best Practices Chunk
    const bpData = {
      score: toon.scores.bestPractices,
      issues: toon.bestPractices
    };

    // 5. Diagnostics Chunk
    const diagData = {
      summary: toon.diagnostics
    };

    // Run parallel execution
    const [performance, accessibility, seo, bestPractices, diagnostics] = await Promise.all([
      this.analyzeCategory("Performance", perfData),
      this.analyzeCategory("Accessibility", accData),
      this.analyzeCategory("SEO", seoData),
      this.analyzeCategory("Best Practices", bpData),
      this.analyzeCategory("Server & Network Diagnostics", diagData)
    ]);

    return { performance, accessibility, seo, bestPractices, diagnostics };
  }
}