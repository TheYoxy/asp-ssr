import {Hydrate, QueryClient, QueryClientProvider} from "@tanstack/react-query";
import React from "react";
import ReactDOM from "react-dom/client";
import {BrowserRouter} from "react-router-dom";
import {App} from "./App";

// @ts-ignore
const dehydratedState = window.__REACT_QUERY_STATE__;
const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            suspense: true,
        },
    },
});
ReactDOM.hydrateRoot(
    document.getElementById("app") as HTMLElement,
    <React.StrictMode>
        <QueryClientProvider client={queryClient}>
            <Hydrate state={dehydratedState}>
                <BrowserRouter>
                    <App/>
                </BrowserRouter>
            </Hydrate>
        </QueryClientProvider>
    </React.StrictMode>
);
console.log("hydrated");
