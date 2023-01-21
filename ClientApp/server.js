import fs from 'node:fs'
import path from 'node:path'
import {fileURLToPath} from 'node:url'
import express from 'express'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

const isTest = process.env.VITEST
process.env["NODE_TLS_REJECT_UNAUTHORIZED"] = 0;
process.env.MY_CUSTOM_SECRET = 'API_KEY_qwertyuiop'

const hmrPort = 5174

export async function createServer(
    root = process.cwd(),
    isProd = process.env.NODE_ENV === 'production',
) {
    const resolve = (p) => path.resolve(__dirname, p)

    const indexProd = isProd
        ? fs.readFileSync(resolve('dist/index.html'), 'utf-8')
        : ''
    
    const app = express()

    /**
     * @type {import('vite').ViteDevServer}
     */
    let vite
    if (!isProd) {
        console.log('Starting dev server...')
        vite = await (
            await import('vite')
        ).createServer({
            root,
            logLevel: isTest ? 'error' : 'info',
            server: {
                middlewareMode: true,
                watch: {
                    // During tests we edit the files too fast and sometimes chokidar
                    // misses change events, so enforce polling for consistency
                    usePolling: true,
                    interval: 100,
                },
                hmr: {
                    protocol: 'ws',
                    port: hmrPort
                },
                proxy: {
                    '/api': {
                        target: 'https://localhost:5001',
                        changeOrigin: true,
                        secure: false,
                    },
                },
            },
            appType: 'custom',
        })
        // use vite's connect instance as middleware
        app.use(vite.middlewares)
    }

    app.use('*', async (req, res) => {
        try {
            const url = req.originalUrl

            let template, render
            if (!isProd) {
                // always read fresh template in dev
                console.log('Rendering template');
                template = fs.readFileSync(resolve('index.html'), 'utf-8')
                template = await vite.transformIndexHtml(url, template)
                render = (await vite.ssrLoadModule('/src/entry-server.tsx')).render
            } else {
                template = indexProd
                // @ts-ignore
                render = (await import('./dist/server/entry-server.js')).render
            }

            const context = {}
            const [appHtml, state] = await render(url, context)

            if (context.url) {
                // Somewhere a `<Redirect>` was rendered
                return res.redirect(301, context.url)
            }

            const html = template.replace(`<!--app-html-->`, appHtml).replace("<!--react-query-data-->", `window.__REACT_QUERY_STATE__ = ${JSON.stringify(state)};`)

            res.status(200).set({'Content-Type': 'text/html'}).end(html)
        } catch (e) {
            !isProd && vite.ssrFixStacktrace(e)
            console.log(e.stack)
            res.status(500).end(e.stack)
        }
    })

    return {app, vite}
}

if (!isTest) {
    createServer().then(({app}) =>
        app.listen(5173, () => {
            console.log('http://localhost:5173')
        }),
    )
}