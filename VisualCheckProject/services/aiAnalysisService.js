const { AzureOpenAI } = require('openai');
const fs = require('fs-extra');

class AIAnalysisService {
    constructor() {
        this.openai = new AzureOpenAI({
            endpoint: process.env.AZURE_OPENAI_ENDPOINT,
            apiKey: process.env.AZURE_OPENAI_KEY,
            apiVersion: process.env.AZURE_OPENAI_API_VERSION,
            deployment: process.env.AZURE_GPT_DEPLOYMENT
        });
    }

    async analyzeUI(screenshots) {
        const results = [];

        for (const shot of screenshots) {
            const imageData = await fs.readFile(shot.screenshotPath, { encoding: 'base64' });

            const prompt = `
Analyze this webpage screenshot. Focus on UI/UX quality:
- Layout & alignment
- Spacing & whitespace
- Visual hierarchy
- Color palette & harmony
- Typography
- Imagery & icons
- Overall aesthetics
- Responsiveness (based on viewport: ${shot.viewport})

Return a JSON object with:
{
  "scores": { "layout": 0-10, "colors": 0-10, "typography": 0-10, "spacing": 0-10, "overall": 0-10 },
  "issues": ["up to 3 most critical issues only"],
  "suggestions": ["up to 5 most impactful improvement suggestions only"]
}

IMPORTANT: Limit to exactly 3 issues and 5 suggestions maximum.
`;

            const response = await this.openai.chat.completions.create({
                model: process.env.AZURE_GPT_DEPLOYMENT,
                messages: [
                    { role: 'system', content: 'You are an expert UI/UX designer AI.' },
                    {
                        role: 'user',
                        content: [
                            { type: 'text', text: prompt },
                            {
                                type: 'image_url',
                                image_url: {
                                    url: `data:image/png;base64,${imageData}`
                                }
                            }
                        ]
                    }
                ]
            });

            const output = response.choices[0].message.content;
            results.push({
                viewport: shot.viewport,
                analysis: JSON.parse(output),
                domJSON: shot.domJSON
            });
        }

        return results;
    }
}

module.exports = new AIAnalysisService();
