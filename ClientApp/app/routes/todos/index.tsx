import {json, LoaderFunction} from "@remix-run/node";
import {useLoaderData} from "@remix-run/react";
import axios from "~/axios.server";

export const loader: LoaderFunction = async () => {
    const todos = await axios.get(`${process.env.BACKEND_URL}/api/todos`).then(response => response.data);
    return json({message: "Hello from Remix!", todos});
}
export default function Todos() {
    const {message, todos} = useLoaderData();
    return (
        <div style={{fontFamily: "system-ui, sans-serif", lineHeight: "1.4"}}>
            <h1>Todos</h1>

            <ul>
                Todos: {todos.map((todo: any) => <li>{todo.title}</li>)}
            </ul>
        </div>
    );
}
