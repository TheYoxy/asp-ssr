import {dehydrate, QueryClient, QueryClientProvider} from "@tanstack/react-query";
import ReactDOMServer from 'react-dom/server'
import {StaticRouter} from 'react-router-dom/server'
import ssrPrepass from 'react-ssr-prepass'
import {App} from './App'

export async function render(url: string, context: unknown) {
    const queryClient = new QueryClient()
    await queryClient.prefetchQuery(['todos'], () => fetch('https://localhost:5001/api/todos').then(res => res.json()))
    
    const BaseApp = () => (
        <QueryClientProvider client={queryClient}>
            <StaticRouter location={url} context={context}>
                <App/>
            </StaticRouter>
        </QueryClientProvider>);
    try {
        await ssrPrepass(<BaseApp/>)
    } catch (e) {
        console.error(e)
    }

    const htmlContent = ReactDOMServer.renderToString(
        <BaseApp/>
    );

    const dehydratedState = dehydrate(queryClient)
    queryClient.clear();
    return [htmlContent, dehydratedState];
}
