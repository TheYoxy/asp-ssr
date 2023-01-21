import {useQuery} from "@tanstack/react-query";

export default function One() {
    const {data} = useQuery(["todos"], () => fetch("/api/todos").then(res => res.json()));
    return <>
        One
        <pre>{JSON.stringify(data, null, 2)}</pre>
    </>
}