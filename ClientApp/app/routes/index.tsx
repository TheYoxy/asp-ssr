import {json, LoaderFunction} from "@remix-run/node";
import {Outlet, useLoaderData} from "@remix-run/react";

export const loader: LoaderFunction = async ({request}) => {
    const auth = request.headers.get('Proxy-Authorization');
    console.log(auth);
    const todos = await fetch(process.env.BACKEND_URL + '/api/todos', {headers: {Authorization: auth}}).then(res => res.ok ? res.json() : []);
    return json({message: "Hello World", todos});
};
export default function Index() {
    const {message, todos} = useLoaderData();
    return (
        <div style={{fontFamily: "system-ui, sans-serif", lineHeight: "1.4"}}>
            <h1>Welcome to Remix</h1>
            <ul>
                <li>
                    <a
                        target="_blank"
                        href="https://remix.run/tutorials/blog"
                        rel="noreferrer"
                    >
                        15m Quickstart Blog Tutorial
                    </a>
                </li>
                <li>
                    <a
                        target="_blank"
                        href="https://remix.run/tutorials/jokes"
                        rel="noreferrer"
                    >
                        Deep Dive Jokes App Tutorial
                    </a>
                </li>
                <li>
                    <a target="_blank" href="https://remix.run/docs" rel="noreferrer">
                        Remix Docs
                    </a>
                </li>
            </ul>

            <hr></hr>
            <Outlet/>
            <hr></hr>
            <p>{message}</p>
            <ul>
                Todos: {todos.map((todo: any) => <li>{todo.title}</li>)}
            </ul>
        </div>
    );
}
