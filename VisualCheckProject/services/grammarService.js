const { AzureOpenAI } = require('openai');

class GrammarService {
    constructor() {
        this.openai = new AzureOpenAI({
            endpoint: process.env.AZURE_OPENAI_ENDPOINT,
            apiKey: process.env.AZURE_OPENAI_KEY,
            apiVersion: process.env.AZURE_OPENAI_API_VERSION,
            deployment: process.env.AZURE_GPT_DEPLOYMENT
        });
    }

    async checkContent(domContent) {
        const textContent = this.extractTextFromDOM(domContent);

        const prompt = `
Analyze the following website text for grammar, spelling, and language quality issues:

${textContent}

Return a JSON object with:
{
  "score": 0-10,
  "issues": [
    {
      "text": "problematic text snippet",
      "type": "grammar|spelling|punctuation|style",
      "suggestion": "corrected version"
    }
  ],
  "summary": "overall language quality assessment"
}

IMPORTANT: Limit to the 3 most critical issues only.
`;

        const response = await this.openai.chat.completions.create({
            model: process.env.AZURE_GPT_DEPLOYMENT,
            messages: [
                { role: 'system', content: 'You are an expert proofreader and grammar checker.' },
                { role: 'user', content: prompt }
            ]
        });

        const output = response.choices[0].message.content;
        return JSON.parse(output);
    }

    extractTextFromDOM(domContent) {
        const text = domContent
            .replace(/<script[^>]*>[\s\S]*?<\/script>/gi, '')
            .replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '')
            .replace(/<[^>]+>/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();
        
        return text.substring(0, 4000);
    }
}

module.exports = new GrammarService();
