import {Suspense} from "react";
import {Link, Route, Routes} from "react-router-dom";
import './index.css'

// Auto generates routes from files under ./pages
// https://vitejs.dev/guide/features.html#glob-import
const pages = import.meta.glob(["./pages/*.jsx", "./pages/*.tsx"], {eager: true});

const routes = Object.keys(pages).map((path) => {
    const name = path.match(/\.\/pages\/(.*)\.[jt]sx$/)[1];
    return {
        name,
        path: name === "Home" ? "/" : `/${name.toLowerCase()}`,
        component: pages[path].default,
    };
});

export function App() {
    return (
        <>
            <nav>
                <ul>
                    {routes.map(({name, path}) => {
                        return (
                            <li key={path}>
                                <Link to={path}>{name}</Link>
                            </li>
                        );
                    })}
                </ul>
            </nav>
            <Suspense fallback={<>Loading....</>}>
                <Routes>
                    {routes.map(({path, component: RouteComp}) => {
                        return <Route key={path} path={path} element={<RouteComp/>}></Route>;
                    })}
                </Routes>
            </Suspense>
        </>
    );
}
