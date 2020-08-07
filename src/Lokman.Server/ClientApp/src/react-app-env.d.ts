/// <reference types="react-scripts" />

// add some useful helpers for react defines
declare namespace NodeJS {
  interface ProcessEnv {
    REACT_APP_HASH: string
    REACT_APP_API_URI: string
    REACT_APP_WS_URI: string
  }
}
// allow class, because we are using React 16+
declare namespace React {
  interface HTMLAttributes<T> extends AriaAttributes, DOMAttributes<T> {
    class?: string;
  }

  interface SVGAttributes<T> extends AriaAttributes, DOMAttributes<T> {
    class?: string;
  }
}
