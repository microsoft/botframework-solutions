import '@babel/polyfill';
import 'dotenv/config';
import envJSON from '../env.json';

process.env = { ...process.env, ...envJSON };

import { createServer } from 'restify';
import { join } from 'path';
import { version } from '../package.json';
import fetch from 'node-fetch';
import random from 'math-random';
import serveHandler from 'serve-handler';

(async function (port, directLineSecret, speechServicesKey, speechServicesRegion) {
  const server = createServer();

  server.get('/health.txt', (_, res) => {
    res.send({
      now: Date.now(),
      version
    });
  });

  server.get('/ready.txt', (_, res) => {
    // TODO: We should add a more reliable readiness check (for cordon in Kubernetes)
    res.send({
      now: Date.now(),
      version
    });
  });

  server.post('/api/directline/token', async (_, res) => {
    // TODO: We should rate-limit to prevent abuse
    try {
      const userID = `dl_${ random().toString(36).substr(2, 10) }`;
      const tokenRes = await fetch('https://directline.botframework.com/v3/directline/tokens/generate', {
        body: JSON.stringify({
          User: { Id: userID }
        }),
        headers: {
          authorization: `Bearer ${ directLineSecret }`,
          'content-type': 'application/json'
        },
        method: 'POST'
      });

      if (!tokenRes.ok) {
        return res.send(503, { message: 'failed to exchange Direct Line secret', innerStatus: tokenRes.status });
      }

      const { expires_in: expiresIn, token } = JSON.parse(await tokenRes.text());

      res.send({ expiresIn, token });
    } catch ({ message }) {
      res.send(500, { message });
    }
  });

  server.post('/api/speechservices/token', async (_, res) => {
    // TODO: We should rate-limit to prevent abuse
    try {
      const tokenRes = await fetch(`https://${ speechServicesRegion }.api.cognitive.microsoft.com/sts/v1.0/issueToken`, {
        headers: {
          'Ocp-Apim-Subscription-Key': speechServicesKey
        },
        method: 'POST'
      });

      if (!tokenRes.ok) {
        return res.send(503, { message: 'failed to exchange Speech Services token', innerStatus: otkenRes.status });
      }

      const token = await tokenRes.text();

      res.send({ expiresIn: 300000, token });
    } catch ({ message }) {
      res.send(500, { message });
    }
  });

  server.get('/**/*', (req, res) => {
    serveHandler(req, res, {
      public: join(__dirname, '../_site')
    });
  });

  server.listen(port, '0.0.0.0', () => {
    console.log(`Server is now listening to port ${ port }`);
    console.log(`- Direct Line secret is "${ directLineSecret.substr(0, 3) }...${ directLineSecret.substr(-3) }"`);
    console.log(`- Speech Services key is "${ speechServicesKey.substr(0, 3) }...${ speechServicesKey.substr(-3) }" of "${ speechServicesRegion }"`);
  });
})(
  process.env.PORT || 80,
  process.env.DIRECT_LINE_SECRET,
  process.env.SPEECH_SERVICES_KEY,
  process.env.SPEECH_SERVICES_REGION
).catch(err => console.error(err));
