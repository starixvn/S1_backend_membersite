import axios from "axios";

//const rootURL = "http://localhost:60930";
const rootURL =
	process.env.NODE_ENV !== "production" ? "http://www.n88dev.com/" : "";

export default () =>
	axios.create({
		baseURL: `${rootURL}`
	});
