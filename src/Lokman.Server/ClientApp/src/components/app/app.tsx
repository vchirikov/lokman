import * as React from "react";

export interface HelloWorldProps {
  userName: string;
  lang: string;
}
export const App = (props: HelloWorldProps) => (
  <h1>
    Hi {props.userName} from React! Welc123123omeaaaaaaaaaaa to {props.lang}!
  </h1>
);
